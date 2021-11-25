using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public interface ITreeSortMethod
    {
        int CompareNodes(InventoryBase x, InventoryBase y, TreeNode nodeX, TreeNode nodeY);

        string Name { get; }
        string Description { get; }
    }
}
