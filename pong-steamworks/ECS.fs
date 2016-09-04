module ECS

open Lidgren.Network
open Microsoft.Xna.Framework
open System.Collections
open System.Collections.Specialized

open ECSTypes
open HelperFunctions

let NetBufferAppearance (appr : Appearance) =
    let netBuffer = new NetBuffer()
    netBuffer.Write(appr.texture)
    netBuffer.Write(appr.size.X)
    netBuffer.Write(appr.size.Y)
    netBuffer

let NetBufferPosition (pos : Position) =
    let netBuffer = new NetBuffer() 
    netBuffer.Write(pos.X)
    netBuffer.Write(pos.Y)
    netBuffer
    
let NetBufferVelocity (vel : Velocity) =
    let netBuffer = new NetBuffer()
    netBuffer.Write(vel.X)
    netBuffer.Write(vel.Y)
    netBuffer

let NetBufferSnapshot (snapshot : Snapshot) =
    let netBuffer = new NetBuffer()
    
    //128-bit chunkIndicies
    let chunkIndicies = new BitArray(entityChunkIndicies)
    for indexChunk in snapshot.entityChunks do
        chunkIndicies.[indexChunk.Key] <- true
    
    let bitArrayBuffer = new NetBuffer()

    //chunk 128-bit bitarray into 4 BitVector32 structs
    let mutable chunkOne = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i
        if chunkIndicies.[actualIndex] = true then
            chunkOne.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data) 
    netBuffer.Write(chunkOne.Data)

    let mutable chunkTwo = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i + 32
        if chunkIndicies.[actualIndex] = true then
            chunkTwo.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data) 
    netBuffer.Write(chunkTwo.Data)

    let mutable chunkThree = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i + 64
        if chunkIndicies.[actualIndex] = true then
            chunkThree.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data)
    netBuffer.Write(chunkThree.Data)

    let mutable chunkFour = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i + 96
        if chunkIndicies.[actualIndex] = true then
            chunkFour.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data)
    netBuffer.Write(chunkFour.Data)

    netBuffer.Write(bitArrayBuffer)

    for i in 0..entityLimit - 1 do
        match Dictionary.TryFind i snapshot.position with
        | Some x -> netBuffer.Write(NetBufferPosition x)
        | None -> ()
        match Dictionary.TryFind i snapshot.appearance with
        | Some x -> netBuffer.Write(NetBufferAppearance x)
        | None -> ()

    netBuffer

let EntityChunkAndBitOffset entity =
    assert (entity < entityLimit)
    let rec EntityChunk x =
        if entity - entityChunkBitSize < 0 then
            0
        else
            1 + EntityChunk (entity - entityChunkBitSize) 
               
    let entChunk = EntityChunk entity
    (entChunk, entity - (entChunk * entChunk))
            
let EntityExists entity entityManager =
    let (chunkIndex, bitOffset) = EntityChunkAndBitOffset entity
    (entityManager.entities.[chunkIndex] |> snd).[bitOffset]

//Finds lowest id to assign to entity, updates chunks, and returns the new entity.
//IO int
let CreateEntity (entityManager : EntityManager) =
    let mutable newEntity = entityLimit
    for i = 0 to entityLimit do
        let (chunkIndex, bitOffset) = EntityChunkAndBitOffset i 
        if not <| (entityManager.entities.[chunkIndex] |> snd).[bitOffset] then
            let mutable chunk = (entityManager.entities.[chunkIndex] |> snd)
            chunk.[bitOffset] <- true
            entityManager.entities.[chunkIndex] <- (true, chunk)

            newEntity <- i

    assert (newEntity <> entityLimit)
    newEntity

//IO ()
let KillEntity entity (entityManager : EntityManager) =
    let (chunkIndex, bitOffset) = EntityChunkAndBitOffset entity
    let mutable chunk = (entityManager.entities.[chunkIndex] |> snd)

    chunk.[bitOffset] <- false
    entityManager.entities.[chunkIndex] <- (true, chunk)

let DeltaSnapshot (client : Client) (baseline : EntityManager) =
    // Step 1: Figure out entity chunks to send
    let updateFlag = Array.map fst baseline.entities

    let snapshotsAfterLatestAcked =
        let reversedSnapshots = List.rev client.snapshots
        Seq.takeWhile (fun x -> x.clientAcknowledged = false) reversedSnapshots

    for snapshot in snapshotsAfterLatestAcked do
        for indexChunk in snapshot.entityChunks do
            updateFlag.[indexChunk.Key] <- true
    
    let snapshotEntityChunks = new Generic.Dictionary<ChunkIndex, BitVector32>()
    for i = 0 to entityChunkIndicies do
        if updateFlag.[i] = true then
            snapshotEntityChunks.Add(i, (snd baseline.entities.[i]))
                
    //Get latest acked snapshot
    let latestAckedSnapshot = 
        match client.snapshots |> List.rev |> List.tryFind (fun x -> x.clientAcknowledged = true) with
            | Some x -> x
            | None -> DummySnapshot()
        
    let snapshotPosition = new Generic.Dictionary<Entity, Position>()
    let snapshotAppearance = new Generic.Dictionary<Entity, Appearance>()
    // Step 2: Add components which have changed.
    for entity = 0 to entityLimit do
        let (chunkIndex, bitOffset) = EntityChunkAndBitOffset entity
        if (baseline.entities.[chunkIndex] |> snd).[bitOffset] = true then
            if not (latestAckedSnapshot.position.ContainsKey(entity)) ||
                baseline.position.[entity] <> latestAckedSnapshot.position.[entity] then
                snapshotPosition.Add(entity, baseline.position.[entity])
            if not (latestAckedSnapshot.appearance.ContainsKey(entity)) ||
                baseline.appearance.[entity] <> latestAckedSnapshot.appearance.[entity] then
                snapshotAppearance.Add(entity, baseline.appearance.[entity])

    {
        entityChunks = snapshotEntityChunks
        position = snapshotPosition
        appearance = snapshotAppearance
        clientAcknowledged = false
    }

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
                //clients' <- List.filter (fun x -> x <> message.SenderConnection) clients'
                printfn "Server: Client from %s has disconnected." (message.SenderEndPoint.ToString())
            | _ ->
                printfn "Server: Client from %s has an unhandled status." (message.SenderEndPoint.ToString())
           
        | NetIncomingMessageType.DebugMessage -> 
            ()
        | _ ->
            eprintfn "Server: Unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- serverSocket.ReadMessage()

//Systems

//updates entities with position and velocity
let private RunMovement (dt:float) (posComponents:Generic.Dictionary<Entity, Position>) velComponents =
    let advance (pos:Position) (vel:Velocity) = ( pos + (float32 dt * vel) : Position)

    let entities = Generic.List<Entity>(posComponents.Keys)
    for entID in entities do
        let velocity = Dictionary.TryFind entID velComponents

        if Option.isSome velocity then
            posComponents.[entID] <- advance posComponents.[entID] (Option.get velocity) 