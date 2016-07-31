module ECS

open Microsoft.Xna.Framework

open ECSTypes
open HelperFunctions

let EntityChunkAndRelativeIndex entity =
    assert (entity < entityLimit)
    let rec EntityChunk x =
        if entity - entityChunkSize < 0 then
            0
        else
            1 + EntityChunk (entity - entityChunkSize) 
               
    let entChunk = EntityChunk entity
    (entChunk, entity - (entChunk * entChunk))
            
//Finds lowest id to assign to entity, updates chunks, and returns the new entity.
let CreateEntity (entityManager : EntityManager) =
    let mutable newEntity = entityLimit
    for i = 0 to entityLimit do
        if not <| entityManager.entities.Contains(i) then
            entityManager.entities.Add(i) |> ignore
            newEntity <- i
            assert (newEntity <> entityLimit)

    let entChunkAndIndex = EntityChunkAndRelativeIndex newEntity
    entityManager.entityChunks.[fst entChunkAndIndex].[snd entChunkAndIndex] <- true
    entityManager.entityChunkUpdateFlag.[fst entChunkAndIndex] <- true
    
    newEntity

let KillEntity entity (entityManager : EntityManager) =
    entityManager.entities.Remove entity |> ignore

    //marks entity chunks for an update
    let entChunkAndIndex = EntityChunkAndRelativeIndex entity
    entityManager.entityChunks.[fst entChunkAndIndex].[snd entChunkAndIndex] <- false
    entityManager.entityChunkUpdateFlag.[fst entChunkAndIndex] <- true

let ComponentMask entID entityManager =
    let mutable buffer = 0
    if entityManager.position.ContainsKey(entID) then
        buffer <- buffer + int ComponentBit.Position
    if entityManager.velocity.ContainsKey(entID) then
        buffer <- buffer + int ComponentBit.Velocity
    if entityManager.appearance.ContainsKey(entID) then
        buffer <- buffer + int ComponentBit.Appearance
        
    if buffer = 0 then
        eprintfn "ComponentMask: Entity %s has no components!" <| entID.ToString()
        System.Diagnostics.Debugger.Launch() |> ignore
        System.Diagnostics.Debugger.Break()
    buffer