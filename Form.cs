namespace LSUF.AutoReports
{
    internal class Form : FormBase
    {
        public Form(string id, string name, string to, string from, string cc, string bcc, string frequency)
        {
            ID = id;
            Filename = GenerateFilename(name);
            To = to;
            From = from;
            CC = cc;
            BCC = bcc;
            Frequency = frequency;
            File = Build(id, Program.IModulesCredentials);
        }
    }
}