using System.Windows.Forms;
using OpenMetaverse;

namespace METAbolt
{
    public interface ITreeSortMethod
    {
        int CompareNodes(InventoryBase x, InventoryBase y, TreeNode nodeX, TreeNode nodeY);

        string Name { get; }
        string Description { get; }
    }
}
