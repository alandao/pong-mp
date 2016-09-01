let (x: uint32 array) = Array.zeroCreate 1
let y = new System.Collections.BitArray(32, true)
y.CopyTo(x, 0)
x