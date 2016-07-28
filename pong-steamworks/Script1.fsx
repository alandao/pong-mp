let a = 128
let b = 999999
let c = 10

let mutable test = System.BitConverter.GetBytes(a)
test <- System.BitConverter.GetBytes(b)
test
test <- System.BitConverter.GetBytes(c)
