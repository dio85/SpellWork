using MySql.Data.MySqlClient;
using SpellWork.Properties;
using SpellWork.Spell;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SpellWork.Database
{
    public static class MySqlConnection
    {
        public static bool Connected { get; private set; }
        public static List<string> Dropped = new List<string>();
        public static List<SpellProcEntry> SpellProcEvent = new List<SpellProcEntry>();

        private static string ConnectionString
        {
            get
            {
                if (Settings.Default.Host == ".")
                    return
                        $"Server=localhost;Pipe={Settings.Default.PortOrPipe};UserID={Settings.Default.User};Password={Settings.Default.Pass};Database={Settings.Default.WorldDbName};CharacterSet=utf8mb4;ConnectionTimeout=5;ConnectionProtocol=Pipe;";

                return
                    $"Server={Settings.Default.Host};Port={Settings.Default.PortOrPipe};UserID={Settings.Default.User};Password={Settings.Default.Pass};Database={Settings.Default.WorldDbName};CharacterSet=utf8mb4;ConnectionTimeout=5;";
            }
        }

        private static string GetSpellName(uint id)
        {
            if (DBC.DBC.SpellInfoStore.ContainsKey((int)id))
                return DBC.DBC.SpellInfoStore[(int)id].NameAndSubname;

            Dropped.Add($"DELETE FROM `spell_proc` WHERE `SpellId`={id};\r\n");
            return string.Empty;
        }

        public static void SelectProc(string query)
        {
            Dropped.Clear();
            using (var conn = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand(query, conn))
                {
                    SpellProcEvent.Clear();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var spellId = reader.GetInt32(0);
                            SpellProcEvent.Add(new SpellProcEntry
                            {
                                SpellId              = spellId,
                                SpellName            = GetSpellName((uint)Math.Abs(spellId)),
                                SchoolMask           = (SpellSchoolMask)reader.GetByte(1),
                                SpellFamilyName      = (SpellFamilyNames)reader.GetUInt16(2),
                                SpellFamilyMask      = new[]
                                {
                                    reader.GetUInt32(3),
                                    reader.GetUInt32(4),
                                    reader.GetUInt32(5),
                                    reader.GetUInt32(6)
                                },
                                ProcFlags            = (ProcFlags)reader.GetUInt32(7),
                                SpellTypeMask        = (ProcFlagsSpellType)reader.GetUInt32(8),
                                SpellPhaseMask       = (ProcFlagsSpellPhase)reader.GetUInt32(9),
                                HitMask              = (ProcFlagsHit)reader.GetUInt32(10),
                                AttributesMask       = (ProcAttributes)reader.GetUInt32(11),
                                DisableEffectsMask   = reader.GetUInt32(12),
                                ProcsPerMinute       = reader.GetFloat(13),
                                Chance               = reader.GetFloat(14),
                                Cooldown             = reader.GetUInt32(15),
                                Charges              = reader.GetByte(16)
                            });
                        }
                    }
                }
            }
        }

        public static void Insert(string query)
        {
            if (Settings.Default.DbIsReadOnly)
                return;

            using (var conn = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (var command = new MySqlCommand(query, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void TestConnect()
        {
            if (!Settings.Default.UseDbConnect)
            {
                Connected = false;
                return;
            }

            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    conn.Close();
                }
                Connected = true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Errno {ex.Number}{Environment.NewLine}{ex.Message}");
                Connected = false;
            }
            catch
            {
                Connected = false;
            }
        }
    }
}
