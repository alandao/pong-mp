module NetBuffer

open Lidgren.Network
open System.Collections
open System.Collections.Specialized

open ECSTypes
open ECS
open HelperFunctions


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

let NetBufferSnapshot (snapshot : Snapshot) =
    let netBuffer = new NetBuffer()    
    let bitArrayBuffer = new NetBuffer()

    for i in 0..entityChunkIndicies - 1 do
        match Dictionary.TryFind i snapshot.entityChunks with
        | Some x ->
            netBuffer.Write(true)
            bitArrayBuffer.Write(x.Data)
        | None ->
            netBuffer.Write(false)

    netBuffer.WritePadBits() //need to do this to align bytes after writing bits
    if bitArrayBuffer.Data <> null then //maybe no elements were created or destroyed.
        netBuffer.Write(bitArrayBuffer)

    for i in 0..entityLimit - 1 do
        match Dictionary.TryFind i snapshot.position with
        | Some x -> netBuffer.Write(NetBufferPosition x)
        | None -> ()
        match Dictionary.TryFind i snapshot.appearance with
        | Some x -> netBuffer.Write(NetBufferAppearance x)
        | None -> ()

    netBuffer
