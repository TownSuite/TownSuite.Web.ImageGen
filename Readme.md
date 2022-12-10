Simple image generation.

1. Identicons
2. Placeholders
3. Proxy

Images are cached on disk and never again generated unless they are deleted.   Deletion can happen manually or in the case of containers, by stopping the running container instance.

The default configuration sets a maximum request size of 1000 x 1000.  
The default configuration places the cache folder as 'wwwroot/cache'.

The configuration can be modified by editing appsettings.json or overriding the settings with environment variables.

Supported image formats are:
1. png
2. jpg
3. gif
4. webp

The default image format is png.

# Identicon example

A default request with size 80.   This generates a png.
```
/avatar/{thehash}
```

Request a different size identicon.
```
/avatar/{thehash}?s=300
```

Request a jpeg identicon.
```
/avatar/{thehash}?imgformat=jpg
/avatar/{thehash}?imgformat=jpeg
```

# Placeholders

A default placeholder request with a width of 80 and height of 80.   This generates a png.

```
/placeholder/{thehash}
```

A request with a width of 800 and a height of 400.  This generates a png.
```
/placeholder/{thehash}?w=800&h=400
```

Request a jpeg placeholder.

```
/placeholder/{thehash}?imgformat=jpeg
```


# Image Proxy

The image proxy can be used to perform resizes of images.   The src image must be url encoded.   

A default proxy call that returns a png with the image original size.

```
/proxy/{url of the source image}
```

A proxy call that returns a jpeg with the image original size.

```
/proxy/{url of the source image}?imgformat=jpeg
```


Request a jpeg and resize to 800x400. Specifying just a width or height will keep the aspect ratio intact.
```
/proxy/{url of the source image}?imgformat=jpeg&w=800&h=400
```


