# VibeWare terminal artwork

`vibeware-symbol.rgb` is a 20-by-20 RGB rendering of the owner-supplied
`vibeware-symbol-dark.png`. It keeps the supplied floppy-disk symbol and colors,
crops the surrounding background, and averages source pixels for terminal cells.
The source PNG SHA-256 is
`6CC171A8C60D3E92DE29FF0B45F9DE9F54E73532CEB5AE229720E0FD672FF535`.

About and General Setup render this static artwork through the existing
Spectre.Console canvas. Narrow and colorless terminals show the brand name and
the owner-supplied [manifesto link](https://umbertogiacobbi.biz/vibeware/manifesto).
No image library, animation, terminal image protocol, or network request is needed.
