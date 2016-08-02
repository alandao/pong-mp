module ECSNetworkServer

open Lidgren.Network
open System.Collections

open ECSTypes
open ServerTypes

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

let NetBufferEntityComponents (entity : Entity) (componentMask : uint32) (baseline : EntityManager) =
    assert(componentMask <> 0u)

    let netBuffer = new NetBuffer()

    netBuffer.Write(componentMask)

    let needsUpdate x = (componentMask &&& (uint32 x)) <> 0u

    //must be alphabetically checked, since client will read in alphabetical order
    if ComponentBit.Appearance |> needsUpdate then
        let appr = baseline.appearance.[entity]
        netBuffer.Write(NetBufferAppearance appr)
    if ComponentBit.Position |> needsUpdate then
        let pos = baseline.position.[entity]
        netBuffer.Write(NetBufferPosition pos)
    if ComponentBit.Velocity |> needsUpdate then
        let vel = baseline.velocity.[entity]
        netBuffer.Write(NetBufferVelocity vel)

    netBuffer

let NetBufferEntityChunks (client : Client) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()

    //chunks which weren't acked will be resent with latest chunk data
    let updateFlag = new BitArray(baseline.entityChunkUpdateFlag)

    let snapshotsAfterLatestAcked = 
        let reversedSnapshots = List.rev client.snapshots
        Seq.takeWhile (fun x -> x.clientAcknowledged = false) reversedSnapshots
            
    for snapshot in snapshotsAfterLatestAcked do
        updateFlag.Or(updateFlag) |> ignore

    //write chunk update flags
    for x in updateFlag do
        netBuffer.Write(x)

    //write chunks
    let mutable i = 0
    for x in updateFlag do
        if x = true then
            for y in baseline.entityChunks.[i] do
                netBuffer.Write(y)
        i <- i + 1

    netBuffer
        
