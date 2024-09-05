﻿using SkiaSharp;

namespace BlazorWebassembly.Pages.skiasharp.Commands
{
    public abstract class DrawElement
    {
        public abstract void Draw(SKCanvas canvas);
    }
}
