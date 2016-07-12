module ECS

open Microsoft.Xna.Framework

open ECSTypes
open HelperFunctions


let CreateEntity () = System.Guid.NewGuid()

let KillEntity entID (entityManager : EntityManager) =
    (   entityManager.entities.Remove entID &&
        entityManager.position.Remove entID &&
        entityManager.velocity.Remove entID &&
        entityManager.appearance.Remove entID
    ) |> ignore

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