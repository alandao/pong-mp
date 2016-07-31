let updateFlag = new System.Collections.BitArray( [| true; false; true |] )
updateFlag
let x = new System.Collections.BitArray([|false; true; false|])
updateFlag.Or(x)
updateFlag