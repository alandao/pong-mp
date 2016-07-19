module ECSTypes

open Microsoft.Xna.Framework
open System.Collections.Generic

//  Component data
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : string; size : Vector2 }

//determines what components of the entity are synced
type NetworkComponentMask = uint32




//used for sending schema info over the internet
type ComponentBit =
    | Position = 1u
    | Velocity = 2u
    | Appearance = 4u

//determines what components were changed during delta compression
type ComponentDiffMask = uint32

type Entity = System.Guid

type EntityManager =
    {
        entities : HashSet<Entity>
        //will hold all possible components
        position : Dictionary<Entity, Position>
        velocity : Dictionary<Entity, Velocity>
        appearance: Dictionary<Entity, Appearance>

        //server use
        network: Dictionary<Entity, NetworkComponentMask>
    }
let emptyEntityManager =
    {
        entities = new HashSet<Entity>()
        //will hold all possible components
        position = new Dictionary<Entity, Position>()
        velocity = new Dictionary<Entity, Velocity>()
        appearance = new Dictionary<Entity, Appearance>()

        network = new Dictionary<Entity, NetworkComponentMask>()
    }