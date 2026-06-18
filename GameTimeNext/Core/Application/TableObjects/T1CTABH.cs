using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1CTABH : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public string TXTYP { get; set; }

        [UIXSignatureField(1)]
        public string DESCR { get; set; }

        [UIXSignatureField(2)]
        public string PERMI { get; set; }

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; }

        [UIXSignatureField(5)]
        public bool PAAC1 { get; set; }

        [UIXSignatureField(6)]
        public string PADE1 { get; set; }

        [UIXSignatureField(7)]
        public bool PARF1 { get; set; }

        [UIXSignatureField(8)]
        public string PACO1 { get; set; }

        [UIXSignatureField(9)]
        public string PACT1 { get; set; }

        [UIXSignatureField(10)]
        public bool PAAC2 { get; set; }

        [UIXSignatureField(11)]
        public string PADE2 { get; set; }

        [UIXSignatureField(12)]
        public bool PARF2 { get; set; }

        [UIXSignatureField(13)]
        public string PACO2 { get; set; }

        [UIXSignatureField(14)]
        public string PACT2 { get; set; }

        public T1CTABH()
        {
            TXTYP = string.Empty;
            DESCR = string.Empty;
            PERMI = string.Empty;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            PAAC1 = false;
            PADE1 = string.Empty;
            PARF1 = false;
            PACO1 = string.Empty;
            PACT1 = string.Empty;

            PAAC2 = false;
            PADE2 = string.Empty;
            PARF2 = false;
            PACO2 = string.Empty;
            PACT2 = string.Empty;

            AcceptChanges();
        }

        public T? GetValue<T>(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return default;

            var pi = GetType().GetProperty(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.IgnoreCase);

            if (pi == null)
                return default;

            var value = pi.GetValue(this);
            if (value == null)
                return default;

            if (value is T typed)
                return typed;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public override void Save()
        {
            TXCTABH tblmCtabh = new TXCTABH();
            tblmCtabh.Save(this);
        }
    }
}