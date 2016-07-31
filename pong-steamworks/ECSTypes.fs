module ECSTypes

open Microsoft.Xna.Framework
open System.Collections
open System.Collections.Generic

//  Component data
type Appearance = { texture : string; size : Vector2 }

type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

//determines what components of the entity are synced
type NetworkComponentMask = uint32



//used for sending schema info over the internet
type ComponentBit =
    | Position = 1u
    | Velocity = 2u
    | Appearance = 4u

type Entity = int

let entityChunkTotal = 128
let entityChunkSize = 32
let entityLimit = entityChunkTotal * entityChunkSize

type EntityManager =
    {
        entities : HashSet<Entity>
        //will hold all possible components
        position : Dictionary<Entity, Position>
        velocity : Dictionary<Entity, Velocity>
        appearance: Dictionary<Entity, Appearance>

        network : Dictionary<Entity, NetworkComponentMask>
        entityChunkUpdateFlag : BitArray //each bit in array determines whether 32-bit chunk needs to be sent
        entityChunks : BitArray array //each bit determines whether entity with ID equal to index exists
    }
let emptyEntityManager =
    {
        entities = new HashSet<Entity>()
        //will hold all possible components
        position = new Dictionary<Entity, Position>()
        velocity = new Dictionary<Entity, Velocity>()
        appearance = new Dictionary<Entity, Appearance>()

        network = new Dictionary<Entity, NetworkComponentMask>()
        entityChunkUpdateFlag = new BitArray(entityChunkTotal)
        entityChunks = Array.create entityChunkTotal (new BitArray(entityChunkSize))
    }