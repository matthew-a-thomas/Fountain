# Fountain

Fountain codes applied to files.

```
dotnet build -c Release Fountain\Fountain.csproj
pushd Fountain\bin\Release\net5.0
test.bat
popd
```

## Things you can do

Turn a file into several fountain files:

```
fountain --encode file.txt file.txt.1.fountain --percent=30
fountain --encode file.txt file.txt.2.fountain --percent=30
fountain --encode file.txt file.txt.3.fountain --percent=30
fountain --encode file.txt file.txt.4.fountain --percent=30
fountain --encode file.txt file.txt.5.fountain --percent=30
```

...and see that there's not much overhead:

```
>dir
1,024   file.txt
  356   file.txt.1.fountain
  356   file.txt.2.fountain
  356   file.txt.3.fountain
  356   file.txt.4.fountain
  356   file.txt.5.fountain
```

...then save the fountain files to various places:

```
move file.txt.1.fountain A:\file.txt.1.fountain
move file.txt.2.fountain B:\file.txt.2.fountain
move file.txt.3.fountain C:\file.txt.3.fountain
move file.txt.4.fountain D:\file.txt.4.fountain
move file.txt.5.fountain E:\file.txt.5.fountain
```

...and throw away the original file:

```
del file.txt
```

...then accidentally lose one of the fountain files:

```
format A:\
```

...but still be able to recover your file!

```
move B:\file.txt.2.fountain file.txt.2.fountain
move C:\file.txt.3.fountain file.txt.3.fountain
move D:\file.txt.4.fountain file.txt.4.fountain
move E:\file.txt.5.fountain file.txt.5.fountain

fountain --merge file.txt.2.fountain

fountain --decode file.txt.2.fountain file.txt

del file.txt.2.fountain
```

## A fountain what?

A fountain code is a forward error correction code, sort of like Reed Solomon but more flexible.

Here's how it works: split your file up into `k` equal parts. Pick a random subset of those `k` parts and XOR them together. That's one row in a fountain file. Once you have enough rows (usually not many more than `k`) then you can decode the fountain file to get your file back.

Here's a gentle introduction to fountain codes:
https://www.matthewathomas.com/programming/2021/07/19/fountain-codes.html

One of the nice things about fountain files is you can turn a single file into multiple fountain files with different numbers of rows in each and save them in different places. Then your file is safe! It doesn't matter which rows survive but as long as roughly `k` of them do then you'll be able to recover your file. If you want your file to be more safe then just generate and store more fountain files.

## Other things you can do

**Check the status of a fountain file**

```
>fountain --info file.txt.fountain
file.txt.fountain
        Source file SHA256: E2-8B-13-...
        Source file size: 127,488 bytes
        Row size: 1,992
        Num coefficients: 64
        Num rows: 1,024
        Solvable in 32,772 steps
```

**Compact a bloated fountain file**
```
fountain --shrink file.txt.fountain
```

## Fountain file structure

There are a few sections in a fountain file.

**Overview** - 48 bytes

 * Magic ASCII string "FNT0". 4 bytes
 * SHA256 hash of source file. 32 bytes
 * File size. 8 byte unsigned integer (Little Endian)
 * Row size. 4 byte unsigned integer (Little Endian)

**Data** - variable bytes

The data section is a sequence of rows, each consisting of:

 * Coefficients. A packed bit field
 * Data. A block of `row size` bytes

The number of coefficients is `ceil(file size / row size)`. The number of bytes in the packed bit field is `ceil(num coefficients / 8)`. Coefficient 0 is to the far left of the bit field.

In other words, each row is `row size + ceil(ceil(file size / row size) / 8)` bytes. And there can be any number of rows (including zero of them).
