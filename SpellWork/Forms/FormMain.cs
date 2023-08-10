using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SpellWork.Database;
using SpellWork.DBC;
using SpellWork.Extensions;
using SpellWork.Spell;

namespace SpellWork.Forms
{
    public sealed partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            splitContainer3.SplitterDistance = 170;

            Text = DBC.DBC.Version;

            _cbSpellFamilyName.SetEnumValues<SpellFamilyNames>("SpellFamilyName");
            _cbSpellAura.SetEnumValues<AuraType>("Aura");
            _cbSpellEffect.SetEnumValues<SpellEffects>("Effect");
            _cbTarget1.SetEnumValues<Targets>("Target A");
            _cbTarget2.SetEnumValues<Targets>("Target B");

            _cbProcSpellFamilyName.SetEnumValues<SpellFamilyNames>("SpellFamilyName");
            _cbProcSpellAura.SetEnumValues<AuraType>("Aura");
            _cbProcSpellEffect.SetEnumValues<SpellEffects>("Effect");
            _cbProcTarget1.SetEnumValues<Targets>("Target A");
            _cbProcTarget2.SetEnumValues<Targets>("Target B");

            _cbProcSpellFamilyTree.SetEnumValues<SpellFamilyNames>("SpellFamilyTree");
            _cbProcFitstSpellFamily.SetEnumValues<SpellFamilyNames>("SpellFamilyName");

            _clbSchools.SetFlags(new[]
            {
                SpellSchoolMask.SPELL_SCHOOL_MASK_NORMAL,
                SpellSchoolMask.SPELL_SCHOOL_MASK_HOLY,
                SpellSchoolMask.SPELL_SCHOOL_MASK_FIRE,
                SpellSchoolMask.SPELL_SCHOOL_MASK_NATURE,
                SpellSchoolMask.SPELL_SCHOOL_MASK_FROST,
                SpellSchoolMask.SPELL_SCHOOL_MASK_SHADOW,
                SpellSchoolMask.SPELL_SCHOOL_MASK_ARCANE
            }, "SPELL_SCHOOL_MASK_");
            _clbProcFlags.SetFlags<ProcFlags>("PROC_FLAG_");
            _clbSpellTypeMask.SetFlags(new[]
            {
                ProcFlagsSpellType.PROC_SPELL_TYPE_DAMAGE,
                ProcFlagsSpellType.PROC_SPELL_TYPE_HEAL,
                ProcFlagsSpellType.PROC_SPELL_TYPE_NO_DMG_HEAL
            }, "PROC_SPELL_TYPE_");
            _clbSpellPhaseMask.SetFlags(new[]
            {
                ProcFlagsSpellPhase.PROC_SPELL_PHASE_CAST,
                ProcFlagsSpellPhase.PROC_SPELL_PHASE_HIT,
                ProcFlagsSpellPhase.PROC_SPELL_PHASE_FINISH
            }, "PROC_SPELL_PHASE_");
            _clbProcFlagHit.SetFlags<ProcFlagsHit>("PROC_HIT_");
            _clbProcAttributes.SetFlags<ProcAttributes>("PROC_ATTR_");

            _cbSqlSpellFamily.SetEnumValues<SpellFamilyNames>("SpellFamilyName");

            _status.Text = String.Format("DBC Locale: {0}", DBC.DBC.Locale);

            _cbAdvancedFilter1.SetStructFields<SpellEntry>();
            _cbAdvancedFilter2.SetStructFields<SpellEntry>();

            _cbAdvancedFilter1CompareType.SetEnumValuesDirect<CompareType>(true);
            _cbAdvancedFilter2CompareType.SetEnumValuesDirect<CompareType>(true);

            ConnStatus();
        }

        #region FORM

        private void ExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AboutClick(object sender, EventArgs e)
        {
            var ab = new FormAboutBox();
            ab.ShowDialog();
        }

        private void TabControl1SelectedIndexChanged(object sender, EventArgs e)
        {
            _cbProcFlag.Visible = _bWrite.Visible = ((TabControl)sender).SelectedIndex == 1;
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            var frm = new FormSettings();
            frm.ShowDialog(this);
            ConnStatus();
        }

        private void FormMainResize(object sender, EventArgs e)
        {
            try
            {
                _scCompareRoot.SplitterDistance = (((Form)sender).Size.Width / 2) - 25;
                _chName.Width = (((Form)sender).Size.Width - 140);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        private void ConnStatus()
        {
            MySqlConnection.TestConnect();

            if (MySqlConnection.Connected)
            {
                _dbConnect.Text = @"Connection successful.";
                _dbConnect.ForeColor = Color.Green;
                // read db data
                DBC.DBC.ItemTemplate = MySqlConnection.SelectItems();
                MySqlConnection.LoadSpellsDBCFromDB();
            }
            else
            {
                _dbConnect.Text = @"No DB Connected";
                _dbConnect.ForeColor = Color.Red;
            }
        }

        private void ConnectedClick(object sender, EventArgs e)
        {
            MySqlConnection.TestConnect();

            if (MySqlConnection.Connected)
                MessageBox.Show(@"Connection is successful!", @"MySQL Connections!", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            else
                MessageBox.Show(@"Connection failed!", @"ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            ConnStatus();
        }

        private void TextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!((Char.IsDigit(e.KeyChar) || e.KeyChar == (char)Keys.Back)))
                e.Handled = true;
        }

        #endregion

        #region SPELL INFO PAGE

        private void LvSpellListSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvSpellList.SelectedIndices.Count > 0)
                new SpellInfo(_rtSpellInfo, _spellList[_lvSpellList.SelectedIndices[0]]);
        }

        private void TbSearchIdKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                AdvancedSearch();
        }

        private void BSearchClick(object sender, EventArgs e)
        {
            AdvancedSearch();
        }

        private void CbSpellFamilyNamesSelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex != 0)
                AdvancedFilter();
        }

        private void TbAdvansedFilterValKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                AdvancedFilter();
        }

        private void AdvancedSearch()
        {
            var name = _tbSearchId.Text;
            var id = name.ToUInt32();
            var ic = _tbSearchIcon.Text.ToUInt32();
            var at = _tbSearchAttributes.Text.ToUInt32();

            _spellList = (from spell in DBC.DBC.Spell.Values
                          where
                              ((id == 0 || spell.ID == id) && (ic == 0 || spell.SpellIconID == ic) &&
                               (at == 0 || (spell.Attributes & at) != 0 || (spell.AttributesEx & at) != 0 ||
                                (spell.AttributesEx2 & at) != 0 || (spell.AttributesEx3 & at) != 0 ||
                                (spell.AttributesEx4 & at) != 0 || (spell.AttributesEx5 & at) != 0 ||
                                (spell.AttributesEx6 & at) != 0 || (spell.AttributesEx7 & at) != 0)) &&
                              ((id != 0 || ic != 0 && at != 0) || spell.SpellName.ContainsText(name))
                          select spell).ToList();

            _lvSpellList.VirtualListSize = _spellList.Count();
            if (_lvSpellList.SelectedIndices.Count > 0)
                _lvSpellList.Items[_lvSpellList.SelectedIndices[0]].Selected = false;
        }

        private void AdvancedFilter()
        {
            var bFamilyNames = _cbSpellFamilyName.SelectedIndex != 0;
            var fFamilyNames = _cbSpellFamilyName.SelectedValue.ToInt32();

            var bSpellAura = _cbSpellAura.SelectedIndex != 0;
            var fSpellAura = _cbSpellAura.SelectedValue.ToInt32();

            var bSpellEffect = _cbSpellEffect.SelectedIndex != 0;
            var fSpellEffect = _cbSpellEffect.SelectedValue.ToInt32();

            var bTarget1 = _cbTarget1.SelectedIndex != 0;
            var fTarget1 = _cbTarget1.SelectedValue.ToInt32();

            var bTarget2 = _cbTarget2.SelectedIndex != 0;
            var fTarget2 = _cbTarget2.SelectedValue.ToInt32();

            // additional filtert
            var advVal1 = _tbAdvancedFilter1Val.Text;
            var advVal2 = _tbAdvancedFilter2Val.Text;

            var field1 = (MemberInfo)_cbAdvancedFilter1.SelectedValue;
            var field2 = (MemberInfo)_cbAdvancedFilter2.SelectedValue;

            var use1Val = advVal1 != string.Empty;
            var use2Val = advVal2 != string.Empty;

            var field1Ct = (CompareType)_cbAdvancedFilter1CompareType.SelectedIndex;
            var field2Ct = (CompareType)_cbAdvancedFilter2CompareType.SelectedIndex;

            _spellList = (from spell in DBC.DBC.Spell.Values
                          where
                              (!bFamilyNames || spell.SpellFamilyName == fFamilyNames) &&
                              (!bSpellEffect || spell.Effect.ContainsElement((uint)fSpellEffect)) &&
                              (!bSpellAura || spell.EffectApplyAuraName.ContainsElement((uint)fSpellAura)) &&
                              (!bTarget1 || spell.EffectImplicitTargetA.ContainsElement((uint)fTarget1)) &&
                              (!bTarget2 || spell.EffectImplicitTargetB.ContainsElement((uint)fTarget2)) &&
                              (!use1Val || spell.CreateFilter(field1, advVal1, field1Ct)) &&
                              (!use2Val || spell.CreateFilter(field2, advVal2, field2Ct))
                          select spell).ToList();

            _lvSpellList.VirtualListSize = _spellList.Count();
            if (_lvSpellList.SelectedIndices.Count > 0)
                _lvSpellList.Items[_lvSpellList.SelectedIndices[0]].Selected = false;
        }

        #endregion

        #region SPELL PROC INFO PAGE

        private void NewProcSpellIdClick(object sender, EventArgs e)
        {
            var spellId = int.Parse(_tbNewProcSpellId.Text);
            var spell = DBC.DBC.Spell[(uint)spellId];
            var proc = new SpellProcEntry()
            {
                SpellId = spellId,
                SpellName = spell.SpellNameRank,
                SpellFamilyName = (SpellFamilyNames)spell.SpellFamilyName,
                SpellFamilyMask = new uint[3],
            };
            ProcParse(proc);
        }

        private void LoadProcFromDBClick(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
            _tbLoadProcSpellId.Text = _tbNewProcSpellId.Text;
        }

        private void NewProcSpellIdTextChanged(object sender, EventArgs e)
        {
            _bNewProcSpellId.Enabled = uint.TryParse(((TextBox)sender).Text, out var spellId) && DBC.DBC.Spell.ContainsKey(spellId);
        }

        private void CbProcSpellFamilyNameSelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex > 0)
                ProcFilter();
        }

        private void CbProcFlagCheckedChanged(object sender, EventArgs e)
        {
            splitContainer3.SplitterDistance = ((CheckBox)sender).Checked ? 270 : 170;
        }

        private void TvFamilyTreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level > 0)
                SetProcAtribute(DBC.DBC.Spell[e.Node.Name.ToUInt32()]);
        }

        private void LvProcSpellListSelectedIndexChanged(object sender, EventArgs e)
        {
            var lv = (ListView)sender;
            if (lv.SelectedIndices.Count <= 0)
                return;
            SetProcAtribute(_spellProcList[lv.SelectedIndices[0]]);
            _lvProcAdditionalInfo.Items.Clear();
        }

        private void LvProcAdditionalInfoSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvProcAdditionalInfo.SelectedIndices.Count > 0)
                SetProcAtribute(DBC.DBC.Spell[_lvProcAdditionalInfo.SelectedItems[0].SubItems[0].Text.ToUInt32()]);
        }

        private void ClbSchoolsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ProcInfo.SpellProc.ID == 0)
                return;
            _bWrite.Enabled = true;
            GetProcAttribute(ProcInfo.SpellProc);
        }

        private void TbCooldownTextChanged(object sender, EventArgs e)
        {
            if (ProcInfo.SpellProc.ID == 0)
                return;
            _bWrite.Enabled = true;
            GetProcAttribute(ProcInfo.SpellProc);
        }

        private void BProcSearchClick(object sender, EventArgs e)
        {
            Search();
        }

        private void TbSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Search();
        }

        private void TvFamilyTreeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex == 0)
                return;
            _tvFamilyTree.Nodes.Clear();
            var spellfamily = (SpellFamilyNames)(((ComboBox)sender).SelectedValue.ToInt32());

            // SPELLFAMILY_GENERIC proc records ignore SpellFamilyMask
            // so we don't populate TreeView for performance
            if (spellfamily == SpellFamilyNames.SPELLFAMILY_GENERIC)
                return;

            new ProcInfo(_tvFamilyTree, spellfamily);
            PopulateProcAdditionalInfo();
        }

        private void SetProcAtribute(SpellEntry spell)
        {
            new SpellInfo(_rtbProcSpellInfo, spell);
        }

        private void GetProcAttribute(SpellEntry spell)
        {
            var SpellFamilyFlags = _tvFamilyTree.GetMask();
            _lProcHeader.Text = $"Spell ({spell.ID}) {spell.SpellNameRank} ==> SpellFamily {_cbProcFitstSpellFamily.SelectedValue}, 0x{SpellFamilyFlags[0]:X8} {SpellFamilyFlags[1]:X8} {SpellFamilyFlags[2]:X8}";
        }

        private void Search()
        {
            var id = _tbProcSeach.Text.ToUInt32();

            _spellProcList = (from spell in DBC.DBC.Spell.Values
                              where
                                  (id == 0 || spell.ID == id) &&
                                  (id != 0 || spell.SpellName.ContainsText(_tbProcSeach.Text))
                              select spell).ToList();

            _lvProcSpellList.VirtualListSize = _spellProcList.Count;
            if (_lvProcSpellList.SelectedIndices.Count > 0)
                _lvProcSpellList.Items[_lvProcSpellList.SelectedIndices[0]].Selected = false;
        }

        private void ProcFilter()
        {
            var bFamilyNames = _cbProcSpellFamilyName.SelectedIndex != 0;
            var fFamilyNames = _cbProcSpellFamilyName.SelectedValue.ToInt32();

            var bSpellAura = _cbProcSpellAura.SelectedIndex != 0;
            var fSpellAura = _cbProcSpellAura.SelectedValue.ToInt32();

            var bSpellEffect = _cbProcSpellEffect.SelectedIndex != 0;
            var fSpellEffect = _cbProcSpellEffect.SelectedValue.ToInt32();

            var bTarget1 = _cbProcTarget1.SelectedIndex != 0;
            var fTarget1 = _cbProcTarget1.SelectedValue.ToInt32();

            var bTarget2 = _cbProcTarget2.SelectedIndex != 0;
            var fTarget2 = _cbProcTarget2.SelectedValue.ToInt32();

            _spellProcList = (from spell in DBC.DBC.Spell.Values
                              where
                                  (!bFamilyNames || spell.SpellFamilyName == fFamilyNames) &&
                                  (!bSpellEffect || spell.Effect.ContainsElement((uint)fSpellEffect)) &&
                                  (!bSpellAura || spell.EffectApplyAuraName.Contains((uint)fSpellAura)) &&
                                  (!bTarget1 || spell.EffectImplicitTargetA.ContainsElement((uint)fTarget1)) &&
                                  (!bTarget2 || spell.EffectImplicitTargetB.ContainsElement((uint)fTarget2))
                              select spell).ToList();

            _lvProcSpellList.VirtualListSize = _spellProcList.Count();
            if (_lvProcSpellList.SelectedIndices.Count > 0)
                _lvProcSpellList.Items[_lvProcSpellList.SelectedIndices[0]].Selected = false;
        }

        private void FamilyTreeAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!ProcInfo.Update)
                return;

            _bWrite.Enabled = true;
            _lvProcAdditionalInfo.Items.Clear();

            PopulateProcAdditionalInfo();

            GetProcAttribute(ProcInfo.SpellProc);
        }

        private void PopulateProcAdditionalInfo()
        {
            _lvProcAdditionalInfo.Items.Clear();
            _lvProcAdditionalInfo.Items.AddRange(
                _tvFamilyTree.Nodes.Cast<TreeNode>()
                    .Where(familyBitNode => familyBitNode.Checked)
                    .SelectMany(familyBitNode => familyBitNode.Nodes.Cast<TreeNode>())
                    .Distinct()
                    .Select(familySpellNode =>
                    {
                        var spell = DBC.DBC.Spell[familySpellNode.Name.ToUInt32()];

                        return new ListViewItem(new[] { familySpellNode.Name, spell.SpellNameRank, spell.Description })
                        {
                            ImageKey = familySpellNode.ImageKey
                        };
                    })
                    .OrderBy(listViewItem => listViewItem.SubItems[0].Text.ToInt32())
                    .ToArray());
        }

        #endregion

        #region COMPARE PAGE

        private void CompareFilterSpellTextChanged(object sender, EventArgs e)
        {
            var spell1 = _tbCompareFilterSpell1.Text.ToUInt32();
            var spell2 = _tbCompareFilterSpell2.Text.ToUInt32();

            if (DBC.DBC.Spell.ContainsKey(spell1) && DBC.DBC.Spell.ContainsKey(spell2))
                new SpellCompare(_rtbCompareSpell1, _rtbCompareSpell2, DBC.DBC.Spell[spell1], DBC.DBC.Spell[spell2]);
        }

        private void CompareSearch1Click(object sender, EventArgs e)
        {
            var form = new FormSearch();
            form.ShowDialog(this);
            if (form.DialogResult == DialogResult.OK)
                _tbCompareFilterSpell1.Text = form.Spell.ID.ToString();
            form.Dispose();
        }

        private void CompareSearch2Click(object sender, EventArgs e)
        {
            var form = new FormSearch();
            form.ShowDialog(this);
            if (form.DialogResult == DialogResult.OK)
                _tbCompareFilterSpell2.Text = form.Spell.ID.ToString();
            form.Dispose();
        }

        #endregion

        #region SQL PAGE

        private void SqlDataListMouseDoubleClick(object sender, MouseEventArgs e)
        {
            ProcParse(sender);
        }

        private void SqlDataListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ProcParse(sender);
        }

        private void SqlToBaseClick(object sender, EventArgs e)
        {
            if (MySqlConnection.Connected)
                MySqlConnection.Insert(_rtbSqlLog.Text);
            else
                MessageBox.Show(@"Can't connect to database!", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SqlSaveClick(object sender, EventArgs e)
        {
            if (_rtbSqlLog.Text == String.Empty)
                return;

            var sd = new SaveFileDialog {Filter = @"SQL files|*.sql"};
            if (sd.ShowDialog() == DialogResult.OK)
                using (var sw = new StreamWriter(sd.FileName, false, Encoding.UTF8))
                    sw.Write(_rtbSqlLog.Text);
        }

        private void CalcProcFlagsClick(object sender, EventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "_bSqlSchool":
                {
                    var val = _tbSqlSchool.Text.ToUInt32();
                    var form = new FormCalculateFlags(typeof(SpellSchools), val, string.Empty);
                    form.ShowDialog(this);
                    if (form.DialogResult == DialogResult.OK)
                        _tbSqlSchool.Text = form.Flags.ToString();
                }
                    break;
                case "_bSqlProc":
                {
                    var val = _tbSqlProc.Text.ToUInt32();
                    var form = new FormCalculateFlags(typeof(ProcFlags), val, "PROC_FLAG_");
                    form.ShowDialog(this);
                    if (form.DialogResult == DialogResult.OK)
                        _tbSqlProc.Text = form.Flags.ToString();
                }
                    break;
                case "_bSqlProcFlagsHit":
                {
                    var val = _tbSqlProcFlagsHit.Text.ToUInt32();
                    var form = new FormCalculateFlags(typeof(ProcFlagsHit), val, "PROC_HIT_");
                    form.ShowDialog(this);
                    if (form.DialogResult == DialogResult.OK)
                        _tbSqlProcFlagsHit.Text = form.Flags.ToString();
                }
                    break;
            }
        }

        private void SelectClick(object sender, EventArgs e)
        {
            if (!MySqlConnection.Connected)
            {
                MessageBox.Show(@"Can't connect to database!", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var compare = _cbBinaryCompare.Checked ? "&" : "=";

            var conditions = new List<string>();
            if (DBC.DBC.Spell.ContainsKey(_tbLoadProcSpellId.Text.ToUInt32()))
                conditions.Add($"SpellId = {_tbLoadProcSpellId.Text.ToInt32()}");

            if (_cbSqlSpellFamily.SelectedValue.ToInt32() != -1)
                conditions.Add($"SpellFamilyName = {_cbSqlSpellFamily.SelectedValue.ToInt32()}");

            if (_tbSqlSchool.Text.ToUInt32() != 0)
                conditions.Add($"SchoolMask {compare} {_tbSqlSchool.Text.ToUInt32()}");

            if (_tbSqlProc.Text.ToUInt32() != 0)
                conditions.Add($"ProcFlags {compare} {_tbSqlProc.Text.ToUInt32()}");

            if (_tbSqlProcFlagsHit.Text.ToUInt32() != 0)
                conditions.Add($"HitMask {compare} {_tbSqlProcFlagsHit.Text.ToUInt32()}");

            var subquery = "WHERE 1=1";
            if (conditions.Count > 0)
                subquery += conditions.Aggregate("", (sql, condition) => sql + " AND " + condition);
            else if (!string.IsNullOrEmpty(_tbSqlManual.Text))
                subquery += " AND " + _tbSqlManual.Text;

            var query = "SELECT SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, "
                        + $"ProcFlags, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, DisableEffectsMask, ProcsPerMinute, Chance, Cooldown, Charges FROM `spell_proc` {subquery} ORDER BY SpellId";
            try
            {
                MySqlConnection.SelectProc(query);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }

            _lvDataList.VirtualListSize = MySqlConnection.SpellProcEvent.Count;
            if (_lvDataList.SelectedIndices.Count > 0)
                _lvDataList.Items[_lvDataList.SelectedIndices[0]].Selected = false;

            // check bad spell and drop
            foreach (var str in MySqlConnection.Dropped)
                _rtbSqlLog.AppendText(str);
        }

        private void WriteClick(object sender, EventArgs e)
        {
            var spellFamilyFlags = _tvFamilyTree.GetMask();

            // spell comment
            var comment = $" -- {ProcInfo.SpellProc.SpellNameRank}";

            // drop query
            var drop = $"DELETE FROM `spell_proc` WHERE `SpellId` IN ({ProcInfo.SpellProc.ID});";

            // insert query
            var procFlags = _clbProcFlags.GetFlagsValue(0) != ProcInfo.SpellProc.ProcFlags ? _clbProcFlags.GetFlagsValue(0) : 0;
            var procPPM = _tbPPM.Text.Replace(',', '.');
            var procChance = _tbChance.Text.Replace(',', '.') != ProcInfo.SpellProc.ProcChance.ToString() ? _tbChance.Text.Replace(',', '.') : "0";
            var procCooldown = _tbCooldown.Text.ToUInt32();
            var procCharges = _tbProcCharges.Text.ToInt32() != ProcInfo.SpellProc.ProcCharges ? _tbProcCharges.Text.ToInt32() : 0;

            var insert = "INSERT INTO `spell_proc` (`SpellId`,`SchoolMask`,`SpellFamilyName`,`SpellFamilyMask0`,`SpellFamilyMask1`,`SpellFamilyMask2`,`ProcFlags`,`ProcFlags2`,`SpellTypeMask`,`SpellPhaseMask`,`HitMask`,`AttributesMask`,`DisableEffectsMask`,`ProcsPerMinute`,`Chance`,`Cooldown`,`Charges`) VALUES\r\n"
                + $"({ProcInfo.SpellProc.ID},0x{_clbSchools.GetFlagsValue():X2},"
                + $"{_cbProcFitstSpellFamily.SelectedValue.ToUInt32()},0x{spellFamilyFlags[0]:X8},0x{spellFamilyFlags[1]:X8},0x{spellFamilyFlags[2]:X8},"
                + $"0x{procFlags:X},0x{_clbSpellTypeMask.GetFlagsValue():X},0x{_clbSpellPhaseMask.GetFlagsValue():X},0x{_clbProcFlagHit.GetFlagsValue():X},0x{_clbProcAttributes.GetFlagsValue():X},0x0,{procPPM},{procChance},{procCooldown},{procCharges});";

            _rtbSqlLog.AppendText(drop + "\r\n" + insert + comment + "\r\n\r\n");
            _rtbSqlLog.ColorizeCode();
            if (MySqlConnection.Connected)
                MySqlConnection.Insert(drop + insert);

            ((Button)sender).Enabled = false;
        }

        private void ProcParse(object sender)
        {
            var proc = MySqlConnection.SpellProcEvent[((ListView)sender).SelectedIndices[0]];
            var spell = DBC.DBC.Spell[(uint)proc.SpellId];
            ProcInfo.SpellProc = spell;

            new SpellInfo(_rtbProcSpellInfo, spell);

            _tbNewProcSpellId.Text = proc.SpellId.ToString();

            _clbSchools.SetCheckedItemFromFlag((uint)proc.SchoolMask);
            _clbProcFlags.SetCheckedItemFromFlag((uint)proc.ProcFlags, 0);
            _clbSpellTypeMask.SetCheckedItemFromFlag((uint)proc.SpellTypeMask);
            _clbSpellPhaseMask.SetCheckedItemFromFlag((uint)proc.SpellPhaseMask);
            _clbProcFlagHit.SetCheckedItemFromFlag((uint)proc.HitMask);
            _clbProcAttributes.SetCheckedItemFromFlag((uint)proc.AttributesMask);

            _cbProcSpellFamilyTree.SelectedValue = (uint)proc.SpellFamilyName;
            _cbProcFitstSpellFamily.SelectedValue = (uint)proc.SpellFamilyName;

            _tbPPM.Text = proc.ProcsPerMinute.ToString(CultureInfo.InvariantCulture);
            _tbChance.Text = proc.Chance.ToString(CultureInfo.InvariantCulture);
            _tbCooldown.Text = proc.Cooldown.ToString();
            _tbProcCharges.Text = proc.Charges.ToString();

            _tvFamilyTree.SetMask(proc.SpellFamilyMask);
            PopulateProcAdditionalInfo();

            tabControl1.SelectedIndex = 1;
        }

        #endregion

        #region VIRTUAL MODE

        private List<SpellEntry> _spellList = new List<SpellEntry>();

        private List<SpellEntry> _spellProcList = new List<SpellEntry>();

        private void LvSpellListRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item =
                new ListViewItem(new[] {_spellList[e.ItemIndex].ID.ToString(), _spellList[e.ItemIndex].SpellNameRank});
        }

        private void LvProcSpellListRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item =
                new ListViewItem(new[]
                                 {_spellProcList[e.ItemIndex].ID.ToString(), _spellProcList[e.ItemIndex].SpellNameRank});
        }

        private void LvSqlDataRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item = new ListViewItem(MySqlConnection.SpellProcEvent[e.ItemIndex].ToArray());
        }

        #endregion
    }
}
