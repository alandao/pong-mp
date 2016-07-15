module ECSTypes

open Microsoft.Xna.Framework
open System.Collections.Generic

//  Component data
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : string; size : Vector2 }

//ComponentBit is used for sending schema info over the internet
type ComponentBit =
    | Position = 0x00000001
    | Velocity = 0x00000002
    | Appearance = 0x00000004


type Entity = System.Guid

type EntityManager =
    {
        entities : HashSet<Entity>
        //will hold all possible components
        position : Dictionary<Entity, Position>
        velocity : Dictionary<Entity, Velocity>
        appearance: Dictionary<Entity, Appearance>
    }
let emptyEntityManager =
    {
        entities = new HashSet<Entity>()
        //will hold all possible components
        position = new Dictionary<Entity, Position>()
        velocity = new Dictionary<Entity, Velocity>()
        appearance = new Dictionary<Entity, Appearance>()
    }