namespace Framework.Script.UI.Anim
{
    /// <summary>
    /// UI 动画对象的自适应方式（把动画的包围盒缩放到挂点的目标尺寸）。
    /// </summary>
    public enum UIAnimFit
    {
        /// <summary>不缩放，保持资源原始尺寸（Spine 里 1 单位 = 1 像素）</summary>
        None = 0,

        /// <summary>按高度等比缩放</summary>
        FitHeight = 1,

        /// <summary>按宽度等比缩放</summary>
        FitWidth = 2,

        /// <summary>等比缩放到完整放进目标区域（留白）</summary>
        Contain = 3,

        /// <summary>等比缩放到铺满目标区域（可能出界）</summary>
        Cover = 4,
    }

    /// <summary>
    /// 动画包围盒的哪个点对齐到挂点（屏幕坐标系：X 向右、Y 向下）。
    /// </summary>
    public enum UIAnimAnchor
    {
        TopLeft = 0,
        TopCenter = 1,
        TopRight = 2,
        MiddleLeft = 3,
        MiddleCenter = 4,
        MiddleRight = 5,
        BottomLeft = 6,
        BottomCenter = 7,
        BottomRight = 8,
    }
}
