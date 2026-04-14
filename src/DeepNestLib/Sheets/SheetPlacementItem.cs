using System.Collections.Generic;

namespace DeepNestLib.Sheets
{
    public class SheetPlacementItem
    {
        public int sheetId;
        public int sheetSource;

        public List<PlacementItem> sheetplacements = new List<PlacementItem>();
        public List<PlacementItem> placements = new List<PlacementItem>();
    }
}
