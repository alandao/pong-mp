module ECS

open Lidgren.Network
open Microsoft.Xna.Framework
open System.Collections
open System.Collections.Specialized

open ECSTypes
open HelperFunctions

let EntityChunkAndBitOffset entity =
    assert (entity < entityLimit)
    let EntityChunk x =
        let rec loop acc x = //tail-recursive
            if x - entityChunkBitSize < 0 then
                acc
            else
                loop (acc + 1) (x - entityChunkBitSize)
        loop 0 x
               
    let entChunk = EntityChunk entity
    (entChunk, BitVector32.CreateMask(entity - (entChunk * entChunk)))
            
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

let DeltaSnapshot clientSnapshots (baseline : EntityManager) =
    // Step 1: Figure out entity chunks to send
    let updateFlag = Array.map fst baseline.entities

    let snapshotsAfterLatestAcked =
        let reversedSnapshots = List.rev clientSnapshots
        Seq.takeWhile (fun x -> x.clientAcknowledged = false) reversedSnapshots

    for snapshot in snapshotsAfterLatestAcked do
        for indexChunk in snapshot.entityChunks do
            updateFlag.[indexChunk.Key] <- true
    
    let snapshotEntityChunks = new Generic.Dictionary<ChunkIndex, BitVector32>()
    for i = 0 to entityChunkIndicies - 1 do
        if updateFlag.[i] = true then
            snapshotEntityChunks.Add(i, (snd baseline.entities.[i]))
                
    //Get latest acked snapshot
    let latestAckedSnapshot = 
        match clientSnapshots |> List.rev |> List.tryFind (fun x -> x.clientAcknowledged = true) with
            | Some x -> x
            | None -> DummySnapshot()
        
    let snapshotPosition = new Generic.Dictionary<Entity, Position>()
    let snapshotAppearance = new Generic.Dictionary<Entity, Appearance>()
    // Step 2: Add components which have changed.
    for entity = 0 to entityLimit - 1 do
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


//Systems

//updates entities with position and velocity
let RunMovement (posComponents:Generic.Dictionary<Entity, Position>) velComponents (dt:float) =
    let advance (pos:Position) (vel:Velocity) = ( pos + (float32 dt * vel) : Position)

    let entities = Generic.List<Entity>(posComponents.Keys)
    for entID in entities do
        let velocity = Dictionary.TryFind entID velComponents

        if Option.isSome velocity then
            posComponents.[entID] <- advance posComponents.[entID] (Option.get velocity) 