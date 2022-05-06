using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Xml;
using System.Collections.Generic;

namespace Vlasov_v2_1d
{
    #region Grid and boundary
    internal struct VelGrid
    {
        public readonly string vmin;
        public readonly string vmax;
        public readonly string vngrid;

        public string Name { get; set; }

        public VelGrid(string vmin, string vmax, string vngrid)
        {
            FormatCheck.CheckDouble(vmin);
            FormatCheck.CheckDouble(vmax);
            FormatCheck.CheckInteger(vngrid);
            FormatCheck.CheckIntNonPositive(vngrid);

            this.vmin = vmin;
            this.vmax = vmax;
            this.vngrid = vngrid;

            Name = "";
        }
    }

    internal struct Grid
    {
        public readonly string xlength;
        public readonly string xngrid;

        public readonly string tstep;
        public readonly string ntsteps;

        public readonly List<VelGrid> velGrids;

        public bool isGPU;

        public Grid(string xlength, string xngrid, string tstep, string ntsteps)
        {
            FormatCheck.CheckDouble(xlength);
            FormatCheck.CheckInteger(xngrid);

            FormatCheck.CheckDouble(tstep);
            FormatCheck.CheckInteger(ntsteps);

            FormatCheck.CheckDoubleNonPositive(xlength);
            FormatCheck.CheckIntNonPositive(xngrid);

            FormatCheck.CheckDoubleNonPositive(tstep);
            FormatCheck.CheckIntNonPositive(ntsteps);

            this.xlength = xlength;
            this.xngrid = xngrid;
            this.tstep = tstep;
            this.ntsteps = ntsteps;

            isGPU = false;

            velGrids = new List<VelGrid>();
        }

        public void AddVelGrids(string vmin, string vmax, string vngrid, string name)
        {
            VelGrid velGrid = new VelGrid(vmin, vmax, vngrid)
            {
                Name = name
            };
            velGrids.Add(velGrid);
        }

        public void AddVelGrids(List<VelGrid> velGrids)
        {
            this.velGrids.Clear();
            foreach (VelGrid item in velGrids)
                this.velGrids.Add(item);
        }

        public void RemoveVelGrids(string name)
        {
            var rm = from vel in velGrids
                     where name == vel.Name
                     select vel;

            foreach (VelGrid item in rm)
                velGrids.Remove(item);
        }
    }

    internal struct Boundary
    {
        public readonly List<string> boundary_values;
        public readonly List<bool> boundary_types;

        public readonly bool periodic;
        public readonly bool interpolator;

        public Boundary(bool periodic, bool interpolator)
        {

            boundary_types = new List<bool>();
            boundary_values = new List<string>();

            this.periodic = periodic;
            this.interpolator = interpolator;
        }

        public void SetBoundaryValues(string left, string right)
        {
            FormatCheck.CheckDouble(left);
            FormatCheck.CheckDouble(right);

            boundary_values.Add(left);
            boundary_values.Add(right);
        }

        public void SetBoundaryType(bool left_type, bool right_type)
        {
            boundary_types.Add(left_type);
            boundary_types.Add(right_type);
        }
    }

    internal struct GridBoundaryConfigs
    {
        public readonly Grid grid;
        public readonly Boundary boundary;

        public GridBoundaryConfigs(Grid grid, Boundary boundary)
        {
            this.grid = grid;
            this.boundary = boundary;
        }
    }
    #endregion

    #region Extra configs
    internal struct Filtration
    {
        public readonly string period;
        public readonly string order;
        public readonly string width;

        public readonly bool enable;

        public Filtration(string period, string order,
                          string width, bool enable)
        {
            if (enable)
            {
                FormatCheck.CheckInteger(period);
                FormatCheck.CheckDouble(order);
                FormatCheck.CheckDouble(width);

                FormatCheck.CheckIntNonPositive(period);
                FormatCheck.CheckDoubleNonPositive(order);
                FormatCheck.CheckDoubleNonPositive(width);
            }

            this.period = period;
            this.order = order;
            this.width = width;
            this.enable = enable;
        }
    }

    internal struct DiagVar
    {
        public readonly string name;
        public readonly string rate;

        public readonly List<string> distrNames;

        public DiagVar(string name, string rate)
        {
            FormatCheck.CheckInteger(rate);
            FormatCheck.CheckIntNonPositive(rate);

            this.name = name;
            this.rate = rate;

            distrNames = new List<string>();
        }

        public void SetDistrNames(string name)
        {
            distrNames.Add(name);
        }
    }

    internal struct Diagnostics
    {
        public readonly string path;
        public readonly List<DiagVar> diagVars;

        public readonly bool enable;

        public void SetDiagVariables(ListBox.ObjectCollection collection)
        {
            foreach (var item in collection)
            {
                bool nonexist = (from var in diagVars
                                 where var.name == item.ToString()
                                 select true).Count() == 0;
                if (nonexist)
                {
                    string[] var = item.ToString().Split(new char[] { '-' });
                    diagVars.Add(new DiagVar(var[0], var[1]));
                }
                    
            }
            
        }

        public void SetDiagVariables(XmlNodeList vars)
        {
            foreach (XmlElement item in vars)
            {
                bool nonexist = (from var in diagVars
                                 where var.name == item["name"].InnerText
                                 select true).Count() == 0;
                if (nonexist)
                    diagVars.Add(new DiagVar(item["name"].InnerText, item["rate"].InnerText));
            }
        }

        public Diagnostics(string path, bool enable)
        {
            diagVars = new List<DiagVar>();

            //FormatCheck.CheckFileExist(path);

            this.path = path;
            this.enable = enable;
        }
    }

    internal struct Field
    {
        public readonly string electric;
        public readonly string magnetic;
        public readonly string name;

        public Field(string electric, string magnetic, string name)
        {
            FormatCheck.CheckExternalField(electric);
            FormatCheck.CheckExternalField(magnetic);

            this.electric = electric;
            this.magnetic = magnetic;
            this.name = name;
        }
    }

    internal struct ExternalField
    {
        public readonly List<Field> fields;

        public readonly bool enable;

        public ExternalField(bool enable)
        {
            fields = new List<Field>();
            this.enable = enable;
        }

        public void SetField(string electric, string magnetic, string name)
        {
            fields.Add(new Field(electric, magnetic, name));
        }

        public void SetField(List<Field> fields)
        {
            this.fields.Clear();
            foreach (Field item in fields)
                this.fields.Add(item);
        }

        public void RemoveField(string name)
        {
            var rm = from field in fields
                     where name == field.name
                     select field;

            foreach (Field item in rm)
                fields.Remove(item);
        }
    }

    internal struct ExtraConfigs
    {
        public readonly Filtration filtration;
        public readonly Diagnostics diagnostics;
        public readonly ExternalField external;

        public ExtraConfigs(Filtration filtration, Diagnostics diagnostics, ExternalField external)
        {
            this.filtration = filtration;
            this.diagnostics = diagnostics;
            this.external = external;
        }
    }
    #endregion

    internal struct Particle
    {
        private string charge;
        private string cmratio;
        private string density;

        private readonly List<string> Xdistr;
        private readonly List<string> Vdistr;

        public bool isCollectDistr;

        public string Name
        {
            set;
            get;
        }

        public string Charge
        {
            set
            {
                FormatCheck.CheckDouble(value);
                charge = value;
            }
            get
            {
                return charge;
            }
        }

        public string Cmratio
        {
            set
            {
                FormatCheck.CheckDouble(value);
                cmratio = value;
            }
            get
            {
                return cmratio;
            }
        }

        public string Density
        {
            set
            {
                FormatCheck.CheckDouble(value);
                FormatCheck.CheckDoubleNonPositive(value);
                density = value;
            }
            get
            {
                return density;
            }
        }

        public void SetXdistr(XmlNodeList xdistrs)
        {
            foreach (XmlElement item in xdistrs)
            {
                Xdistr.Add(item.InnerText);
            }
        }

        public void SetVdistr(XmlNodeList vdistrs)
        {
            foreach (XmlElement item in vdistrs)
            {
                Vdistr.Add(item.InnerText);
            }
        }

        public void RemoveXdistr(string distr)
        {
            if (!string.IsNullOrEmpty(distr) && Xdistr.Contains(distr))
                Xdistr.Remove(distr);
        }

        public void AddXdistr(string distr)
        {
            FormatCheck.CheckDistrFcn(distr);
            if (!string.IsNullOrEmpty(distr))
                Xdistr.Add(distr);
        }

        public void RemoveVdistr(string distr)
        {
            if (!string.IsNullOrEmpty(distr) && Vdistr.Contains(distr))
                Vdistr.Remove(distr);
        }

        public void AddVdistr(string distr)
        {
            FormatCheck.CheckDistrFcn(distr);
            if (!string.IsNullOrEmpty(distr))
                Vdistr.Add(distr);
        }

        public List<string> GetXdistr()
        {
            return Xdistr;
        }

        public List<string> GetVdistr()
        {
            return Vdistr;
        }

        public Particle(string name, string charge,
                        string cmratio, string density)
        {
            Xdistr = new List<string>();
            Vdistr = new List<string>();

            Name = name;

            FormatCheck.CheckDouble(charge);
            FormatCheck.CheckDouble(cmratio);
            FormatCheck.CheckDouble(density);

            FormatCheck.CheckDoubleNonPositive(density);

            this.charge = charge;
            this.cmratio = cmratio;
            this.density = density;

            isCollectDistr = false;
        }

    }


    internal class VlasovInternalException : Exception
    {
        public VlasovInternalException(string message) : base(message)
        {

        }

    }

    internal static class FormatCheck
    {
        public static void CheckDouble(string input)
        {
            if (!double.TryParse(input, out _))
                throw new VlasovInternalException("Format error, cannot convert to double.");
        }

        public static void CheckInteger(string input)
        {
            if (!int.TryParse(input, out _))
                throw new VlasovInternalException("Format error, connot convert to integer.");
        }

        public static void CheckIntNonPositive(string input)
        {
            if (int.Parse(input) <= 0)
                throw new VlasovInternalException("Input must be a positive integer.");
        }

        public static void CheckDoubleNonPositive(string input)
        {
            if (double.Parse(input) <= 0)
                throw new VlasovInternalException("Input must be a positive double.");
        }

        public static void CheckFileExist(string input)
        {
            if (!File.Exists(input))
                throw new VlasovInternalException("File does not exist.");
        }

        public static void CheckDistrFcn(string input)
        {
            if (!input.Contains("@(x)") && !input.Contains("@(vx)"))
                throw new VlasovInternalException("Input is not a valid matlab" +
                    " handle or does not have valid arguments.");
        }

        public static void CheckExternalField(string input)
        {
            input = input.Replace(" ", "");
            if (!input.Contains("@(t,x)"))
                throw new VlasovInternalException("Input is not a valid matlab " +
                    "handle or does not have valid arguments.");
        }
    }

    internal static class Constants
    {
        public static readonly string[] gridVarNames =
            { "xmax", "xngrids" };
        public static readonly string[] tempVarNames =
            { "tstep", "ntsteps" };
        public static readonly string[] boundaryVarNames =
            { "left", "right" };
    }
}