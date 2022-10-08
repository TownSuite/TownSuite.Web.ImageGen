Simple image generation.

1. Identicons
2. Placeholders

Images are cached on disk and never again generated unless they are deleted.   Deletion can happen manually or in the case of containers, by stopping the running container instance.

The default configuration sets a maximum request size of 1000 x 1000.  
The default configuration places the cache folder as 'wwwroot/cache'.

The configuration can be modified by editing appsettings.json or overriding the settings with environment variables.


# Identicon example

A default request with size 80.   This generates a png.
```
/avatar/{thehash}
```

A dequest a different size identicon.
```
/avatar/{thehash}?s=300
```

# Placeholders

Default placeholder request with a width of 80 and height of 80.   This generates a png.

```
/placeholder/{thehash}
```

A request with a width of 800 and a height of 400.  This generates a png.
```
/placeholder/{thehash}?w=800&h=400
```
