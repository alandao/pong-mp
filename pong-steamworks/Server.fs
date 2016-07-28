module Server

open Microsoft.Xna.Framework
open System.Collections.Generic
open System.Collections.Specialized
open Lidgren.Network

open HelperFunctions
open SharedServerClient
open ECS
open ECSTypes
open ECSNetworkServer

let entityChunks = 128
let entityChunksByteArraySize =
    let bytesize = 8
    assert (entityChunks % bytesize = 0)
    entityChunks / bytesize

let entityChunkBitSize = 32 //do not change. Chunks are size 32 because we use BitVector32 which is always 32-bits
let entityMaxCount = entityChunks * entityChunkBitSize //4096 if entityChunks = 128 and entityChunkBitSize = 32

//Types

//Snapshots are what the server sends to a client to update their gamestate
type Snapshot =
    {
        entityExistenceDiffBitMask : byte array
        entityExistenceChunks : BitVector32 list
        entities : EntityManager
        clientAcknowledged : bool
    }

//The server keeps track of the last 32 snapshots it sent to the client
let snapshotBufferSize = 32
type Client = NetConnection * Queue<Snapshot>

let DummySnapshot() = 
    {
        entityExistenceDiffBitMask = Array.zeroCreate entityChunksByteArraySize
        entityExistenceChunks = List.Empty
        entities = emptyEntityManager
        clientAcknowledged = true
    }

let DeltaEntity (entity : Entity) (snapshot : Snapshot) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()

    netBuffer.Write(entity)

    let willSync x = (baseline.network.[entity] &&& (uint32 x)) <> 0u
    let mutable entityDiffMask : ComponentDiffMask = 0u

    if ComponentBit.Appearance |> willSync then
        if (not <| snapshot.entities.appearance.ContainsKey(entity) || 
                (snapshot.entities.appearance.[entity] <> baseline.appearance.[entity])) then
            entityDiffMask <- entityDiffMask + uint32 ComponentBit.Appearance
    if ComponentBit.Position |> willSync then
        if (not <| snapshot.entities.position.ContainsKey(entity) || 
                (snapshot.entities.position.[entity] <> baseline.position.[entity])) then
            entityDiffMask <- entityDiffMask + uint32 ComponentBit.Position 
    if ComponentBit.Velocity |> willSync then
        if (not <| snapshot.entities.position.ContainsKey(entity) || 
                (snapshot.entities.velocity.[entity] <> baseline.velocity.[entity])) then
            entityDiffMask <- entityDiffMask + uint32 ComponentBit.Velocity

    netBuffer.Write(entityDiffMask)

    let needsUpdate x = (entityDiffMask &&& (uint32 x)) <> 0u

    if ComponentBit.Appearance |> needsUpdate then
        let appr = baseline.appearance.[entity]
        netBuffer.Write(NetBufferAppearance appr)
    if ComponentBit.Position |> needsUpdate then
        let pos = baseline.position.[entity]
        netBuffer.Write(NetBufferPosition pos)
    if ComponentBit.Velocity |> needsUpdate then
        let vel = baseline.velocity.[entity]
        netBuffer.Write(NetBufferVelocity vel)

    netBuffer

let DeltaEntityExistenceBitMask

let DeltaSnapshot (snapshot : Snapshot) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()
    // Write delta compressed entity existence bit string

    

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

