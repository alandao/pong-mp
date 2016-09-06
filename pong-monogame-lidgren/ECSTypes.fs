module ECSTypes

open Microsoft.Xna.Framework
open Lidgren.Network
open System.Collections
open System.Collections.Specialized

//  COMPONENTS
type Appearance = { texture : string; size : Vector2 }

type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

//determines what components of the entity are synced
type NetworkComponentMask = uint32

//  END COMPONENTS

//  Server Message types
type ServerMessage =
    | Snapshot = 0
    | Schema = 1

//used for sending schema info over the internet
type ComponentBit =
    | Position = 1u
    | Velocity = 2u
    | Appearance = 4u

type Entity = int
type ChunkIndex = int
type ChunkEdited = bool

//Don't change bit size of chunks. We're using BitVector32 everywhere
let entityChunkBitSize = 32

let entityChunkIndicies = 128
let entityLimit = entityChunkIndicies * entityChunkBitSize

//properties of EntityManager may be modified each server tick
type EntityManager =
    {
        position : Generic.Dictionary<Entity, Position>
        velocity : Generic.Dictionary<Entity, Velocity>
        appearance : Generic.Dictionary<Entity, Appearance>
 
        entities : (ChunkEdited * BitVector32) array // //first element in tuple determines whether 32-bit chunk needs to be sent
        //each bit offset determines whether entity with ID equal to chunkIndex*chunkSize + offset exists

    }
let EmptyEntityManager() =
    {
        //will hold all possible components
        position = new Generic.Dictionary<Entity, Position>()
        velocity = new Generic.Dictionary<Entity, Velocity>()
        appearance = new Generic.Dictionary<Entity, Appearance>()


        entities = 
            let newArray = Array.zeroCreate entityChunkIndicies
            for i = 0 to newArray.Length - 1 do
                newArray.[i] <- (false, new BitVector32(0))
            newArray
    }

//ClientGameState has all the info that a client needs for displaying graphics and sound.
type ClientGamestate =
    {
        position : Generic.Dictionary<Entity, Position>
        appearance : Generic.Dictionary<Entity, Appearance>
    }

//Snapshots are what the server sends to a client to update their gamestate
type Snapshot =
    {
        entityChunks : Generic.Dictionary<int, BitVector32>
        position : Generic.Dictionary<Entity, Position>
        appearance : Generic.Dictionary<Entity, Appearance>
        clientAcknowledged : bool
    }
let DummySnapshot() = 
    {
        entityChunks = new Generic.Dictionary<Entity, BitVector32>()
        position = new Generic.Dictionary<Entity, Position>()
        appearance = new Generic.Dictionary<Entity, Appearance>()
        clientAcknowledged = true 
    }

//The server keeps track of the last 32 snapshots it sent to the client
let snapshotBufferSize = 32
type ServerState =
    {
        entityManager : EntityManager
        clients : Generic.Dictionary<NetConnection, Snapshot list>
    }
let EmptyServerState() =
    {
        entityManager = EmptyEntityManager()
        clients = new Generic.Dictionary<NetConnection, Snapshot list>()
    }