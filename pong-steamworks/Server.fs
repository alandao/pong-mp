module Server

open Microsoft.Xna.Framework
open System.Collections.Generic
open Lidgren.Network

open HelperFunctions
open SharedServerClient
open ECS
open ECSTypes

//Types

//Snapshots are what the server sends to a client to update their gamestate
type Snapshot =
    {
        entities : EntityManager
        clientAcknowledged : bool
    }

//The server keeps track of the last 32 snapshots it sent to the client
type Client = NetConnection * Queue<Snapshot>

let DummySnapshot() = 
    {
        entities = emptyEntityManager
        //clientAcknowledged isn't required state here
        clientAcknowledged = true
    }


let WriteDelta (entity : Entity) (snapshot : Snapshot) (baseline : EntityManager) (netBuffer : NetBuffer) =
    let isSynced x = (baseline.network.[entity] &&& (uint32 x)) <> 0u

    netBuffer.Write(entity.ToByteArray())

    let mutable componentDiff : ComponentDiffMask = 0u

    if ComponentBit.Appearance |> isSynced then
        if (snapshot.entities.appearance.[entity] <> baseline.appearance.[entity]) then
            componentDiff <- componentDiff + uint32 ComponentBit.Appearance
    if ComponentBit.Position |> isSynced then
        if (snapshot.entities.position.[entity] <> baseline.position.[entity]) then
            componentDiff <- componentDiff + uint32 ComponentBit.Position        
    if ComponentBit.Velocity |> isSynced then
        if (snapshot.entities.velocity.[entity] <> baseline.velocity.[entity]) then
            componentDiff <- componentDiff + uint32 ComponentBit.Velocity

    netBuffer.Write(componentDiff)

    let isChanged x = (componentDiff &&& (uint32 x)) <> 0u
    if ComponentBit.Appearance |> isChanged then
        () //Write appearance
    if ComponentBit.Position |> isChanged then
        ()
    if ComponentBit.Velocity |> isChanged then
        ()

let DeltaCompress (lastAckedSnapshot : Snapshot) (baseline : ServerEntityManager) =
    let (>>=) m f = Option.bind f m

    let SyncComponentsToNewSnapshot entity (mask : SyncedComponentMask) (baseline : EntityManager) (newSnapshot : EntityManager) =
        if (mask &&& int ComponentBit.Appearance) <> 0 then
            newSnapshot.appearance.Add(entity, baseline.appearance.[entity])
        if (mask &&& int ComponentBit.Position) <> 0 then
            newSnapshot.position.Add(entity, baseline.position.[entity])       
        if (mask &&& int ComponentBit.Velocity) <> 0 then
            newSnapshot.velocity.Add(entity, baseline.velocity.[entity])

    let newSnapshot = DummySnapshot()
    for entMaskPair in baseline.synchronizedEntities do
        let baselineEntity = entMaskPair.Key
        let baselineEntityMask = entMaskPair.Value
        if not <| lastAckedSnapshot.synchronizedEntities.ContainsKey(baselineEntity) then
            //this is a new entity, add it and its components from the baseline to the new snapshot
            newSnapshot.synchronizedEntities.Add(baselineEntity, baselineEntityMask)
            newSnapshot.synchronizedEntityManager.entities.Add(baselineEntity) |> ignore
            SyncComponentsToNewSnapshot baselineEntity baselineEntityMask baseline.entityManager newSnapshot.synchronizedEntityManager
        else 
            if lastAckedSnapshot.synchronizedEntities.[baselineEntity] <> baselineEntityMask then
                //we have components to remove or add to the new snapshot for that entity


let SendSnapshotToClients serverEntityManager (clients : Client list) (serverSocket : NetServer) =

    for clientSnapshotTuple in clients do
        let message = serverSocket.CreateMessage()
        let netBuffer = new NetBuffer()
        let client = fst clientSnapshotTuple
        let snapshots = snd clientSnapshotTuple

        netBuffer.Write(byte ServerMessage.Snapshot)
        //get the last acked snapshot for client to diff with baseline
        let lastAckedSnapshot =
            let mutable lastAckedSnapshot' = dummySnapshot()

            for snapshot in snapshots do
                if snapshot.clientAcknowledged then
                    lastAckedSnapshot' <- snapshot
            lastAckedSnapshot'


//Clients will first receive a schema update before receiving world updates 
let SendFullSchemaToClients serverEntityManager clients (serverSocket:NetServer) =
    let message = serverSocket.CreateMessage()
    let netBuffer = new NetBuffer()
    
    netBuffer.Write(byte ServerMessage.Schema)
    netBuffer.Write(serverEntityManager.synchronizedEntities.Count)
    for entMaskPair in serverEntityManager.synchronizedEntities do
        netBuffer.Write(entMaskPair.Key.ToByteArray())
        netBuffer.Write(entMaskPair.Value)
    netBuffer.Write("Schema Update okay!")
    message.Write(netBuffer)

    for client in clients do
        serverSocket.SendMessage(message, client, NetDeliveryMethod.Unreliable) |> ignore
        
let SendFullGameState serverEntityManager clients (serverSocket:NetServer) =
    let WriteSyncedComponents entMaskPair netBuffer =
        let syncedComponentMask = entMaskPair.Value
        if (syncedComponentMask &&& ComponentBit.Appearance)
            netBuffer.Write()

    //for each client, send a full snapshot of the gamestate
    for client in clients do
        let message = serverSocket.CreateMessage()
        let netBuffer = new NetBuffer()
        netBuffer.Write(byte ServerMessage.Snapshot)
        for entMaskPair in serverEntityManager.synchronizedEntities do
            entMaskPair.Value
            //netBuffer.Write(entity)
            
        netBuffer.Write("World Update okay!")
        message.Write(netBuffer)
        serverSocket.SendMessage(message, client, NetDeliveryMethod.Unreliable) |> ignore



//Public facing functions
let StartSocket port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server

let ProcessClientMessages currentClients (serverSocket:NetServer) =
    let mutable newClients:NetConnection list = []

    //process messages from clients
    let mutable message = serverSocket.ReadMessage()
    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printfn "Server: message '%s' received" data
        
        | NetIncomingMessageType.StatusChanged ->
            //A client connected or disconnected.
            match message.SenderConnection.Status with
            | NetConnectionStatus.Connected ->
                newClients <- message.SenderConnection::newClients
                printfn "Server: Client from %s has connected." (message.SenderEndPoint.ToString())
            | NetConnectionStatus.Disconnected ->
                //remove client from connections list
                clients' <- List.filter (fun x -> x <> message.SenderConnection) clients'
                printfn "Server: Client from %s has disconnected." (message.SenderEndPoint.ToString())
            | _ ->
                printfn "Server: Client from %s has an unhandled status." (message.SenderEndPoint.ToString())
           
        | NetIncomingMessageType.DebugMessage -> 
            ()
        | _ ->
            eprintfn "Server: Unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- serverSocket.ReadMessage()

