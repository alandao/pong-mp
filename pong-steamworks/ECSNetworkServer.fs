module ECSNetworkServer

open ECSTypes
open Lidgren.Network

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
