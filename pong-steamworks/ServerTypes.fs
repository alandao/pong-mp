module ServerTypes

open Lidgren.Network
open System.Collections.Generic

open ECSTypes

//Snapshots are what the server sends to a client to update their gamestate
type Snapshot =
    {
        entities : EntityManager
        clientAcknowledged : bool
    }

//The server keeps track of the last 32 snapshots it sent to the client
let snapshotBufferSize = 32
type Client = 
    {
        connection : NetConnection
        snapshots : Snapshot list
    }

let DummySnapshot() = 
    {
        entities = emptyEntityManager
        clientAcknowledged = true
    }