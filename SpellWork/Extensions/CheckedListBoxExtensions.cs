using System;
using System.Windows.Forms;

namespace SpellWork.Extensions
{
    public static class CheckedListBoxExtensions
    {
        sealed class CheckedListBoxEnumItem
        {
            public string Name { get; private set; }
            public uint Value { get; private set; }

            public CheckedListBoxEnumItem(string name, uint value)
            {
                Name = name;
                Value = value;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public static void SetCheckedItemFromFlag(this CheckedListBox name, uint value)
        {
            for (var i = 0; i < name.Items.Count; ++i)
            {
                var item = (CheckedListBoxEnumItem)name.Items[i];
                name.SetItemChecked(i, (value & item.Value) != 0);
            }
        }

        public static uint GetFlagsValue(this CheckedListBox name)
        {
            uint val = 0;
            for (var i = 0; i < name.CheckedItems.Count; ++i)
            {
                var item = (CheckedListBoxEnumItem)name.CheckedItems[i];
                val |= item.Value;
            }

            return val;
        }

        public static void SetFlags<T>(this CheckedListBox clb, string remove = null)
        {
            clb.SetFlags(typeof(T), remove);
        }

        public static void SetFlags(this CheckedListBox clb, Type type, string remove = null)
        {
            clb.SetFlags(Enum.GetValues(type), remove);
        }

        public static void SetFlags(this CheckedListBox clb, Array values, string remove = null)
        {
            clb.Items.Clear();

            foreach (var elem in values)
                clb.Items.Add(new CheckedListBoxEnumItem(elem.ToString().NormalizeString(remove), Convert.ToUInt32(elem)));
        }
    }
}
