module ECSTypes

open Microsoft.Xna.Framework
open Lidgren.Network
open System.Collections.Specialized

//  COMPONENTS
type Appearance = { texture : string; size : Vector2 }

type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)
//  END COMPONENTS

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
        //each bit offset determines whether entity with ID equal to chunkIndex*chunkSize + offset exists 
        entities : Map<ChunkIndex, BitVector32>

        position : Map<Entity, Position>
        velocity : Map<Entity, Velocity>
        appearance : Map<Entity, Appearance>
    }
let EmptyEntityManager() =
    {
        entities = List.fold (fun map i -> Map.add i (new BitVector32()) map) Map.empty [0 .. 127]
             
        //will hold all possible components
        position = Map.empty
        velocity = Map.empty
        appearance = Map.empty
    }

//GameState has all the info that a client needs
type GameState =
    {
        entities : Generic.Dictionary<ChunkIndex, BitVector32>
        position : Generic.Dictionary<Entity, Position>
        appearance : Generic.Dictionary<Entity, Appearance>
    }

//Snapshots are copies of the gamestate in the client history slot
type Snapshot =
    {
        gameState : GameState
        clientAcknowledged : bool
    }
let DummySnapshot() = 
    {
        gameState = 
            {
                entities = 
                    let newDictionary = new Generic.Dictionary<ChunkIndex, BitVector32>()
                    for i = 0 to entityChunkIndicies - 1 do
                        newDictionary.Add(i, new BitVector32(0))
                    newDictionary

                position = new Generic.Dictionary<Entity, Position>()
                appearance = new Generic.Dictionary<Entity, Appearance>()
            }
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