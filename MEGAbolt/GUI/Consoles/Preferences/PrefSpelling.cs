using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using MEGAbolt.Controls;
using System.IO;
using System.Linq;
using System.Reflection;


namespace MEGAbolt
{
    public partial class PrefSpelling : System.Windows.Forms.UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private string lang = string.Empty;
        private Popup toolTip3;
        private CustomToolTip customToolTip;

        public PrefSpelling(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance; 

            GetDictionaries();

            string msg = "Enables spell checking in public chat and IMs.\n\nClick for online help";
            toolTip3 = new Popup(customToolTip = new CustomToolTip(instance, msg))
            {
                AutoClose = false,
                FocusOnOpen = false
            };
            toolTip3.ShowingAnimation = toolTip3.HidingAnimation = PopupAnimations.Blend;

            checkBox1.Checked = instance.Config.CurrentConfig.EnableSpelling;
            lang = instance.Config.CurrentConfig.SpellLanguage;

            label2.Text = $"Selected language: {lang}";

            listBoxLanguage.SelectedItem = lang + ".dic";

            SetFlag();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lang = listBoxLanguage.Items[listBoxLanguage.SelectedIndex].ToString();

            instance.Config.CurrentConfig.SpellLanguage = lang;

            label2.Text = $"Selected language: {lang}";
            SetFlag();
        }

        #region IPreferencePane Members

        string IPreferencePane.Name => "  Spelling";

        Image IPreferencePane.Icon => Properties.Resources.spell_checker;

        void IPreferencePane.SetPreferences()
        {
            instance.Config.CurrentConfig.EnableSpelling = checkBox1.Checked;
            instance.Config.CurrentConfig.SpellLanguage = lang;
        }

        #endregion

        private void GetDictionaries()
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var entry in resources)
            {
                if (!entry.StartsWith("MEGAbolt.Spelling.") || !entry.EndsWith(".dic")) { continue; }

                string chopped = entry.Substring("MEGAbolt.Spelling.".Length);
                chopped = chopped.Substring(0, chopped.Length - ".dic".Length);
                listBoxLanguage.Items.Add(chopped);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
             groupBox1.Enabled = checkBox1.Checked;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = (listBoxLanguage.SelectedIndex != -1);

            SetSelFlag();
        }

        private void SetFlag()
        {
            string[] sfile = lang.Split('-');

            picFlag.Image = ilFlags.Images[sfile[1] + ".png"];
        }

        private void SetSelFlag()
        {
            string sellang = listBoxLanguage.Items[listBoxLanguage.SelectedIndex].ToString();
            string[] sfile = sellang.Split('-');

            sfile = sfile[1].Split('.');

            picFlag.Image = ilFlags.Images[sfile[0] + ".png"];
        }

        private void picSpell_MouseHover(object sender, EventArgs e)
        {
            toolTip3.Show(picSpell);
        }

        private void picSpell_MouseLeave(object sender, EventArgs e)
        {
            toolTip3.Close();
        }
    }
}
