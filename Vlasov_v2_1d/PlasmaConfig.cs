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
    public partial class PlasmaConfig : Form
    {
        private readonly Form1 baseForm;
        private List<Particle> particles;

        public PlasmaConfig(Form1 form)
        {
            InitializeComponent();

            baseForm = form;

            particles = new List<Particle>();
        }

        private void PlasmaConfig_Load(object sender, EventArgs e)
        {
            baseForm.OnPlasmaFormChangedEvent += BaseForm_OnPlasmaFormChangedEvent;
        }

        private void BaseForm_OnPlasmaFormChangedEvent(ref List<Particle> input)
        {
            input = particles;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex >= 0)
            {
                try
                {
                    particles[comboBox1.SelectedIndex].AddXdistr(textBox4.Text);
                    listBox1.Items.Add(textBox4.Text);
                }
                catch (VlasovInternalException ve)
                {
                    MessageBox.Show(ve.Message + " The source is " + ve.Source,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex >= 0)
            {
                try
                {
                    particles[comboBox1.SelectedIndex].AddVdistr(textBox5.Text);
                    listBox2.Items.Add(textBox5.Text);
                }
                catch (VlasovInternalException ve)
                {
                    MessageBox.Show(ve.Message + " The source is " + ve.Source,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1 && comboBox1.SelectedIndex != -1)
            {
                particles[comboBox1.SelectedIndex].RemoveXdistr(
                    (string)listBox1.SelectedItem
                    );
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex != -1 && comboBox1.SelectedIndex != -1)
            {
                particles[comboBox1.SelectedIndex].RemoveVdistr(
                    (string)listBox2.SelectedItem
                    );
                listBox2.Items.RemoveAt(listBox2.SelectedIndex);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();

            int index = comboBox1.SelectedIndex;

            Particle selected = particles[index];

            textBox6.Text = selected.Name;
            textBox1.Text = selected.Charge;
            textBox2.Text = selected.Cmratio;
            textBox3.Text = selected.Density;

            foreach (var item in selected.GetXdistr())
            {
                listBox1.Items.Add(item);
            }

            foreach (var item in selected.GetVdistr())
            {
                listBox2.Items.Add(item);
            }

            checkBox1.Checked = selected.isCollectDistr;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Particle particle;
            try
            {
                particle = new Particle(textBox6.Text, textBox1.Text,
                                        textBox2.Text, textBox3.Text);
            }
            catch (VlasovInternalException ve)
            {
                MessageBox.Show(ve.Message + " The source is " + ve.Source,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            particles.Add(particle);
            textBox7.Text = Convert.ToString(particles.Count);

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            comboBox1.Items.Add(textBox6.Text);
            comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                particles.RemoveAt(comboBox1.SelectedIndex);
                comboBox1.Items.RemoveAt(comboBox1.SelectedIndex);
            }
            else
            {
                return;
            }

            textBox6.Text = "";
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            textBox7.Text = Convert.ToString(particles.Count);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Particle p;

            if (comboBox1.SelectedIndex != -1)
            {
                p = particles[comboBox1.SelectedIndex];
                p.Name = textBox6.Text;

                try
                {
                    p.Charge = textBox1.Text;
                    p.Cmratio = textBox2.Text;
                    p.Density = textBox3.Text;
                }
                catch (VlasovInternalException ve)
                {
                    MessageBox.Show(ve.Message + " The source is " + ve.Source,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                particles[comboBox1.SelectedIndex] = p;

                comboBox1.Items[comboBox1.SelectedIndex] = p.Name;
            }
        }

        internal void SetFieldFromXml(List<Particle> particles)
        {
            this.particles = particles;

            comboBox1.Items.Clear();

            foreach (Particle item in particles)
            {
                comboBox1.Items.Add(item.Name);
            }

            textBox7.Text = Convert.ToString(particles.Count);
        }

        private void checkBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                Particle p = particles[comboBox1.SelectedIndex];
                p.isCollectDistr = checkBox1.Checked;
                particles[comboBox1.SelectedIndex] = p;
            }
        }
    }
}
