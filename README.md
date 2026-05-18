# PudinKiller VFX Texture Lab

A Unity 6+ editor tool for VFX artists to batch-edit grayscale masks, contrast, gradients, packed channels, thresholds, and stylized texture ramps without leaving Unity.

## Features

- Batch texture processing
- Non-destructive output or overwrite mode
- Always-visible original/result preview
- Contrast / Origin Push
- Gradient Mapper
  - Unity Gradient
  - RGBA Curves
  - HSV Curves
- Invert
- Levels
- Threshold
- Posterize
- Colorize
- Channel Pack
- Auto Normalize
- Blur
- Dilate
- Erode
- Data Linear / Color sRGB output modes

## Installation

In Unity:

1. Open `Window > Package Manager`
2. Click `+`
3. Choose `Install package from git URL`
4. Paste: https://github.com/PudinKiller/VFXTextureLab.git

## Usage

Open:

Tools > Pudin Killer > VFX Texture Lab

## Recommended settings

For VFX masks, noise, dissolve maps, flow maps, packed channels:
- Content Type: Data Linear
- Generate Mip Maps: Off
- Force Uncompressed: On
- RGB Single Source: On for grayscale textures

For final visible color textures:
- Content Type: Color sRGB
- Use Gradient Mapper
- Try HSV Curves for vibrant color variation


## License

MIT