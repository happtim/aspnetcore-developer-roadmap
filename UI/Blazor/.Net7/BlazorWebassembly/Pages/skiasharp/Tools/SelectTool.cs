﻿using BlazorWebassembly.Pages.skiasharp.Commands;
using BlazorWebassembly.Pages.skiasharp.Draws;
using SkiaSharp;

namespace BlazorWebassembly.Pages.skiasharp.Tools
{
    public class SelectTool : ITool
    {
        private DrawManager _drawManager;
        private SelectedManager _selectedManager;
        public SelectTool(
            DrawManager drawManager, 
            SelectedManager selectedManager)
        {
            _drawManager = drawManager;
            _selectedManager = selectedManager;

        }

        /// <summary>
        /// 1. 点击空白处，清空选中
        /// 2. 点击元素，选中元素
        /// 3. 点击shift，多选
        /// </summary>
        /// <param name="worldPoint"></param>
        public void MouseDown(SKPoint worldPoint)
        {
            var item = _drawManager.HitTest(worldPoint);
            if (item == null)
            {
                _selectedManager.Clear();
            }
            else
            {
                _selectedManager.Set(item);
            }
            
        }

        public void MouseMove(SKPoint worldPoint)
        {
            var item = _drawManager.HitTest(worldPoint);

            _selectedManager.SetHover(item);
        }

        public void MouseUp(SKPoint worldPoint)
        {

        }
    }
}
