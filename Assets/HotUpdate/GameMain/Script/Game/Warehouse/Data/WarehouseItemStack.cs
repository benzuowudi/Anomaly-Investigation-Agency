namespace GameMain.Script.Warehouse
{
    /// <summary>
    /// 仓库里的一条道具记录（道具ID + 数量）。
    /// 只作为查询结果 / 批量入参 / 存档的载体，不持有权威数据。
    /// </summary>
    public class WarehouseItemStack
    {
        public int m_ItemId;
        public int m_Count;

        public WarehouseItemStack(int _itemId, int _count)
        {
            m_ItemId = _itemId;
            m_Count = _count;
        }

        public override string ToString()
        {
            return "{ itemId:" + m_ItemId + ", count:" + m_Count + " }";
        }
    }
}
