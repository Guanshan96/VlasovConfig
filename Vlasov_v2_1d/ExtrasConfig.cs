using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vlasov_v2_1d
{
    public partial class ExtrasConfig : Form
    {
        private readonly Form1 baseForm;
        private ExtraConfigs extraConfigs;

        private int preVarIndex;
        private int preFieldIndex;
        private List<Field> fields;

        public ExtrasConfig(Form1 form)
        {
            InitializeComponent();

            baseForm = form;
            preVarIndex = 0;
            preFieldIndex = 0;

            fields = new List<Field>();
        }

        private void ExtrasConfig_Load(object sender, EventArgs e)
        {
            baseForm.OnExtraFormChangedEvent += BaseForm_OnFormChangedEvent;

            listBox1.Items.Add("Density");
            listBox1.Items.Add("Velocity");
            listBox1.Items.Add("Distribution");
            listBox1.Items.Add("Total charge");
            listBox1.Items.Add("Total energy");
            listBox1.Items.Add("Electric field");

            if (!checkBox1.Checked)
                groupBox1.Enabled = false;

            if (!checkBox2.Checked)
                groupBox2.Enabled = false;

            if (!checkBox3.Checked)
                groupBox3.Enabled = false;

            listBox1.SelectionMode = SelectionMode.MultiExtended;
            listBox2.SelectionMode = SelectionMode.MultiExtended;
        }

        private void BaseForm_OnFormChangedEvent(ref ExtraConfigs input)
        {
            Filtration filtration = extraConfigs.filtration;
            Diagnostics diagnostics = extraConfigs.diagnostics;
            ExternalField externalField = extraConfigs.external;

            try
            {
                filtration = new Filtration(textBox1.Text, textBox2.Text,
                                            textBox3.Text, checkBox1.Checked);

                diagnostics = new Diagnostics(textBox4.Text, checkBox2.Checked);

                externalField = new ExternalField(checkBox3.Checked);
                externalField.SetField(fields);
            }
            catch (VlasovInternalException ve)
            {
                MessageBox.Show(ve.Message + " The source is " + ve.Source, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            diagnostics.SetDiagVariables(listBox2.Items);

            input = new ExtraConfigs(filtration, diagnostics, externalField);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (string item in listBox1.SelectedItems)
                if (!listBox2.Items.Contains(item))
                {
                    listBox2.Items.Add(item + "-10");
                }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int selected_count = listBox2.SelectedItems.Count;

            if (selected_count != 0)
                for (int i = 0; i < selected_count; i++)
                {
                    int index = listBox2.SelectedIndices[0];
                    listBox2.Items.RemoveAt(index);
                }
        }

        internal void SetFieldFromXml(ExtraConfigs extraConfigs)
        {
            checkBox1.Checked = extraConfigs.filtration.enable;
            textBox1.Text = extraConfigs.filtration.period;
            textBox2.Text = extraConfigs.filtration.order;
            textBox3.Text = extraConfigs.filtration.width;

            checkBox2.Checked = extraConfigs.diagnostics.enable;
            textBox4.Text = extraConfigs.diagnostics.path;

            listBox2.Items.Clear();

            foreach (DiagVar item in extraConfigs.diagnostics.diagVars)
                listBox2.Items.Add(item.name+"-"+item.rate);

            textBox7.Text = extraConfigs.diagnostics.diagVars[0].rate;

            checkBox3.Checked = extraConfigs.external.enable;

            comboBox1.Items.Clear();
            foreach (Field item in extraConfigs.external.fields)
                comboBox1.Items.Add(item.name);

            fields = extraConfigs.external.fields;
            this.extraConfigs = extraConfigs;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                groupBox1.Enabled = true;
            else
                groupBox1.Enabled = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                groupBox2.Enabled = true;
            else
                groupBox2.Enabled = false;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                groupBox3.Enabled = true;
            else
                groupBox3.Enabled = false;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count == 1)
            {
                string var;

                if (preVarIndex != -1)
                {
                    var = listBox2.Items[preVarIndex].ToString();
                    listBox2.Items.RemoveAt(preVarIndex);
                    listBox2.Items.Insert(preVarIndex, var.Substring(0, var.IndexOf('-')) + "-" + textBox7.Text);
                }

                preVarIndex = listBox2.SelectedIndex;

                if (listBox2.SelectedItem != null)
                {
                    var = listBox2.SelectedItem.ToString();
                    textBox7.Text = var.Substring(var.IndexOf('-') + 1);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox5.Text)&&
                !string.IsNullOrEmpty(textBox6.Text))
                fields[preFieldIndex] = new Field(textBox5.Text, textBox6.Text,
                    comboBox1.Items[preFieldIndex].ToString());

            textBox5.Text = fields[comboBox1.SelectedIndex].electric;
            textBox6.Text = fields[comboBox1.SelectedIndex].magnetic;

            preFieldIndex = comboBox1.SelectedIndex;
        }
    }
}
