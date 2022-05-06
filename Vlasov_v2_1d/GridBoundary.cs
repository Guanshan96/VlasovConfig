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
    public partial class GridBoundary : Form
    {
        private readonly Form1 baseForm;

        private Grid grid;
        private Boundary boundary;
        private int preSpecieInd;
        List<VelGrid> velGrids;

        public GridBoundary(Form1 form)
        {
            InitializeComponent();

            baseForm = form;
            preSpecieInd = 0;
            velGrids = new List<VelGrid>();
        }

        private void GridBoundary_Load(object sender, EventArgs e)
        {
            baseForm.OnGridFormChangedEvent += BaseForm_OnFormChangedEvent;

            checkBox2.Checked = false;
        }

        private void BaseForm_OnFormChangedEvent(ref GridBoundaryConfigs input)
        {
            try
            {
                grid = new Grid(textBox1.Text, textBox3.Text, textBox5.Text, textBox6.Text);

                grid.AddVelGrids(velGrids);
                grid.isGPU = checkBox2.Checked;

                boundary = new Boundary(checkBox1.Checked, radioButton1.Checked);

                boundary.SetBoundaryType(radioButton3.Checked, radioButton5.Checked);
                boundary.SetBoundaryValues(textBox7.Text, textBox8.Text);
            }
            catch (VlasovInternalException ve)
            {
                MessageBox.Show(ve.Message + " The source is " + ve.Source, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            input = new GridBoundaryConfigs(grid, boundary);
        }

        internal void SetFieldFromXml(Grid grid, Boundary boundary)
        {
            textBox1.Text = grid.xlength;
            textBox3.Text = grid.xngrid;

            textBox5.Text = grid.tstep;
            textBox6.Text = grid.ntsteps;

            comboBox1.Items.Clear();
            foreach (VelGrid item in grid.velGrids)
            {
                comboBox1.Items.Add(item.Name);
            }

            velGrids = grid.velGrids;

            textBox7.Text = boundary.boundary_values[0];
            textBox8.Text = boundary.boundary_values[1];

            checkBox1.Checked = boundary.periodic;

            if (boundary.interpolator)
            {
                radioButton1.Checked = true;
                radioButton2.Checked = false;
            }
            else
            {
                radioButton1.Checked = false;
                radioButton2.Checked = true;
            }

            radioButton3.Checked = boundary.boundary_types[0];
            radioButton5.Checked = boundary.boundary_types[1];

            checkBox2.Checked = grid.isGPU;

            this.grid = grid;
            this.boundary = boundary;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                groupBox1.Enabled = true;
                groupBox2.Enabled = true;
            }
            else
            {
                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox9.Text) &&
                !string.IsNullOrEmpty(textBox4.Text))
                velGrids[preSpecieInd] = new
                    VelGrid(textBox2.Text, textBox9.Text, textBox4.Text)
                { Name = comboBox1.Items[preSpecieInd].ToString() };

            textBox2.Text = velGrids[comboBox1.SelectedIndex].vmin;
            textBox9.Text = velGrids[comboBox1.SelectedIndex].vmax;
            textBox4.Text = velGrids[comboBox1.SelectedIndex].vngrid;

            preSpecieInd = comboBox1.SelectedIndex;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            
        }
    }
}
