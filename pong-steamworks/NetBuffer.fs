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
    
    //128-bit chunkIndicies
    let chunkIndicies = new BitArray(entityChunkIndicies)
    for indexChunk in snapshot.entityChunks do
        chunkIndicies.[indexChunk.Key] <- true
    
    let bitArrayBuffer = new NetBuffer()

    //chunk 128-bit bitarray into 4 BitVector32 structs
    let mutable chunkOne = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i
        if chunkIndicies.[actualIndex] = true then
            chunkOne.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data) 
    netBuffer.Write(chunkOne.Data)

    let mutable chunkTwo = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i + 32
        if chunkIndicies.[actualIndex] = true then
            chunkTwo.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data) 
    netBuffer.Write(chunkTwo.Data)

    let mutable chunkThree = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i + 64
        if chunkIndicies.[actualIndex] = true then
            chunkThree.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data)
    netBuffer.Write(chunkThree.Data)

    let mutable chunkFour = new BitVector32(0)
    for i in 0..31 do
        let actualIndex = i + 96
        if chunkIndicies.[actualIndex] = true then
            chunkFour.[i] <- true
            bitArrayBuffer.Write(snapshot.entityChunks.[actualIndex].Data)
    netBuffer.Write(chunkFour.Data)

    netBuffer.Write(bitArrayBuffer)

    for i in 0..entityLimit - 1 do
        match Dictionary.TryFind i snapshot.position with
        | Some x -> netBuffer.Write(NetBufferPosition x)
        | None -> ()
        match Dictionary.TryFind i snapshot.appearance with
        | Some x -> netBuffer.Write(NetBufferAppearance x)
        | None -> ()

    netBuffer
