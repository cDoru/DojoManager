﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DojoManager.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace DojoManager.Data
{
    public class DBManager
    {
        public string ConnectionString { get; set; }

        public DBManager(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public DBManager()
        {
            this.ConnectionString = Program.Configuration["AppConfig:AppDB"];
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
        /*
        public List<User> GetAllUsers()
        {
            List<User> list = new List<User>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM Users", conn);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new User()
                        {
                            UserID = reader.GetInt32("ID"),
                            Name = reader.GetString("Name"),
                            Password = reader.GetString("Password"),
                            Email = reader.GetString("Email")
                        });
                    }
                }
            }

            return list;
        }
        */

        private int GetRowsCount(MySqlCommand command)
        {
            int rowsCount = Convert.ToInt32(command.ExecuteScalar());
            return rowsCount;
        }

        public ConfigMailGun GetMailGunConfig()
        {
            ConfigMailGun data = new ConfigMailGun();
            string configValue = "";

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT Value FROM Config where `Key` = 'MailGun'", conn);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        configValue = reader.GetString("Value");
                    }
                }
                conn.Close();
            }

            data = JsonConvert.DeserializeObject<ConfigMailGun>(configValue);

            return data;
        }

        #region User methods

        public string SaveRefreshToken(string email, string refreshToken, DateTime expireDT)
        {
            string status = "";
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        // remove all tokens for the user
                        User user = new User();
                        user.Email = email;
                        user = GetUserDetailsFromEmail(user);


                        // save the new token for the user
                        cmd.Connection = conn;

                        cmd.CommandText = "DELETE from dojo.UserRefreshTokens where `userId` = @USERID";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@USERID", user.UserId);

                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into dojo.UserRefreshTokens(`Token`,`UserId`,`ExpireDT`,`CreatedDT`) VALUES(@TOKEN,@USERID,@EXPIREDT,@CREATEDDT)";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@TOKEN", refreshToken);
                        cmd.Parameters.AddWithValue("@EXPIREDT", expireDT);
                        cmd.Parameters.AddWithValue("@CREATEDDT", DateTime.UtcNow);

                        cmd.ExecuteNonQuery();

                        status = "SUCCESS";
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
                status = "ERROR: " + ex.Message;
            }

            return status;
        }


        public User UserSaveNewUser(User model)
        {
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;

                        cmd.CommandText = "INSERT into dojo.users(`RecordDT`,`FirstName`,`LastName`,`Email`,`EmailConfirmed`" +
                            ",`Password`,`Salt`,`Tel1`,`Tel2`,`Address1`,`Address2`,`City`,`Province`,`Country`,`PostalCode`,`Status`) " +
                            "VALUES (NOW(), @FN, @LN, @Email, @ECON, @PASS, @SALT, @T1, @T2, @A1, @A2, @CITY, @PROV, @COUNTRY, @PC, @STATUS)";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@FN", model.FirstName);
                        cmd.Parameters.AddWithValue("@LN", model.LastName);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@ECON", model.EmailConfirmed);
                        cmd.Parameters.AddWithValue("@PASS", model.Password);
                        cmd.Parameters.AddWithValue("@SALT", model.Salt);
                        cmd.Parameters.AddWithValue("@T1", model.Tel1);
                        cmd.Parameters.AddWithValue("@T2", model.Tel2);
                        cmd.Parameters.AddWithValue("@A1", model.Address1);
                        cmd.Parameters.AddWithValue("@A2", model.Address2);
                        cmd.Parameters.AddWithValue("@CITY", model.City);
                        cmd.Parameters.AddWithValue("@PROV", model.Province);
                        cmd.Parameters.AddWithValue("@COUNTRY", model.Country);
                        cmd.Parameters.AddWithValue("@PC", model.PostalCode);
                        cmd.Parameters.AddWithValue("@STATUS", model.Status);

                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "SELECT * FROM dojo.users WHERE `Email` = @Email";
                        cmd.Prepare();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.UserId = reader.GetInt32("UserId");
                                model.RecordDT = reader.GetDateTime("RecordDT");
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }

            return model;
        }

        public User GetUserDetailsFromEmail(User model)
        {
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT `UserId`,`RecordDT`,`Email`,`FirstName`,`LastName`,`Tel1`,`Tel2`,`Address1`,`Address2`,`City`,`Province`,`Country`,`Status`,`EmailConfirmed` FROM dojo.users WHERE `Email` = @Email";
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Prepare();

                        cmd.ExecuteNonQuery();

                        

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if(!reader.IsDBNull(0)) model.UserId = reader.GetInt32("UserId");
                                if (!reader.IsDBNull(1)) model.RecordDT = reader.GetDateTime("RecordDT");
                                if (!reader.IsDBNull(2)) model.Email = reader.GetString("Email");
                                if (!reader.IsDBNull(3)) model.FirstName = reader.GetString("FirstName");
                                if (!reader.IsDBNull(4)) model.LastName = reader.GetString("LastName");
                                if (!reader.IsDBNull(5)) model.Tel1 = reader.GetString("Tel1");
                                if (!reader.IsDBNull(6)) model.Tel2 = reader.GetString("Tel2");
                                if (!reader.IsDBNull(7)) model.Address1 = reader.GetString("Address1");
                                if (!reader.IsDBNull(8)) model.Address2 = reader.GetString("Address2");
                                if (!reader.IsDBNull(9)) model.City = reader.GetString("City");
                                if (!reader.IsDBNull(10)) model.Province = reader.GetString("Province");
                                if (!reader.IsDBNull(11)) model.Country = reader.GetString("Country");
                                if (!reader.IsDBNull(12)) model.Status = reader.GetInt32("Status");
                                if (!reader.IsDBNull(13)) model.EmailConfirmed = reader.GetInt32("EmailConfirmed");
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return model;
        }


        public List<PermissionFunction> GetUserAllowedRoles(int userId)
        {
            List<PermissionFunction> permissions = new List<PermissionFunction>();
            
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;
                        cmd.CommandText = "select f.FunctionId, f.`Name`, F.Description, F.IsActive from dojo.functions as f " +
                                            "inner join dojo.rolefunctions as rf on rf.FunctionId = f.FunctionId " +
                                            "inner join dojo.userroles as ur on ur.RoleId = rf.RoleId " +
                                            "where ur.UserId = @USERID";
                        cmd.Parameters.AddWithValue("@USERID", userId);
                        cmd.Prepare();

                        cmd.ExecuteNonQuery();



                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PermissionFunction permission = new PermissionFunction();
                                if (!reader.IsDBNull(0)) permission.FunctionId = reader.GetInt32("FunctionId");
                                if (!reader.IsDBNull(1)) permission.Name = reader.GetString("Name");
                                if (!reader.IsDBNull(2)) permission.Description = reader.GetString("Description");
                                if (!reader.IsDBNull(3)) permission.IsActive = reader.GetInt32("IsActive");
                                permissions.Add(permission);
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return permissions;
        }

        public List<PermissionFunction> GetListOfAllowedPermissionsForJWT(string jwt)
        {
            List<PermissionFunction> permissions = new List<PermissionFunction>();
            
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;
                        cmd.CommandText = "select f.FunctionId, f.`Name`, F.Description, F.IsActive from dojo.functions as f " +
                                            "inner join dojo.rolefunctions as rf on rf.FunctionId = f.FunctionId " +
                                            "inner join dojo.userroles as ur on ur.RoleId = rf.RoleId " +
                                            "inner join dojo.users as u on u.UserId = ur.UserId " +
                                            "where u.JWT = @JWT";
                        cmd.Parameters.AddWithValue("@JWT", jwt);
                        cmd.Prepare();

                        cmd.ExecuteNonQuery();



                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PermissionFunction permission = new PermissionFunction();
                                if (!reader.IsDBNull(0)) permission.FunctionId = reader.GetInt32("FunctionId");
                                if (!reader.IsDBNull(1)) permission.Name = reader.GetString("Name");
                                if (!reader.IsDBNull(2)) permission.Description = reader.GetString("Description");
                                if (!reader.IsDBNull(3)) permission.IsActive = reader.GetInt32("IsActive");
                                permissions.Add(permission);
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return permissions;
        }

        public bool DoesUserExist(string email)
        {
            bool result = false;
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;

                        cmd.CommandText = "SELECT * FROM dojo.users WHERE `Email` = @Email";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@Email", email);

                        if (GetRowsCount(cmd) > 0)
                        {
                            result = true;
                        }

                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }


        public bool IsValidRefreshToken(string token)
        {
            bool result = false;
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;

                        cmd.CommandText = "SELECT * FROM dojo.userrefreshtokens WHERE `Token` = @TOKEN and `ExpireDT` >= UTC_TIMESTAMP()";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@TOKEN", token);

                        if (GetRowsCount(cmd) > 0)
                        {
                            result = true;
                        }

                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }


        public int GetUserIdFromEmail(string email)
        {
            int userId = 0;
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;
                        cmd.CommandText = "select `UserId` from dojo.users " +
                                            "where `Email` = @EMAIL";
                        cmd.Parameters.AddWithValue("@EMAIL", email);
                        cmd.Prepare();

                        cmd.ExecuteNonQuery();



                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) userId = reader.GetInt32("UserId");
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return userId;
        }

        public Boolean SaveUserJWT(string email, string jwt)
        {
            int userId = 0;
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;
                        cmd.CommandText = "update dojo.users set `JWT` = @JWT " +
                                            "where `Email` = @EMAIL";
                        cmd.Parameters.AddWithValue("@EMAIL", email);
                        cmd.Parameters.AddWithValue("@JWT", jwt);
                        cmd.Prepare();

                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return true;
        }

        public string ReturnSaltForUserEmail(string email)
        {
            string salt = "";
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;

                        cmd.CommandText = "SELECT UserId, Salt FROM dojo.users WHERE `Email` = @Email";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@Email", email);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                salt = reader.GetString("Salt");
                            }
                        }

                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return salt;
        }

        public bool ValidUserAndPassword(string email, string password)
        {
            bool result = false;
            try
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {

                        cmd.Connection = conn;

                        cmd.CommandText = "SELECT * FROM dojo.users WHERE `Email` = @Email and `Password` = @Password";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", password);

                        if (GetRowsCount(cmd) > 0)
                        {
                            result = true;
                        }

                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }
        #endregion

    }
}
