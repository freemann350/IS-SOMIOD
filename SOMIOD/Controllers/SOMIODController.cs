using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SOMIOD.App_Start;
using SOMIOD.Models;

using Application = SOMIOD.Models.Application;
using Container = SOMIOD.Models.Container;

namespace SOMIOD.Controllers
{
    [Route("api/somiod")]
    public class SOMIODController : ApiController
    {
        private string connStr = Properties.Settings.Default.connectionString;
        MqttPublisher mqttService = new MqttPublisher("127.0.0.1");
        
        private int GetAppID(string application)
        {
            string sql_get_application = "SELECT Id FROM Applications WHERE name = @name";
            SqlConnection conn = null;
            int app_id = 0;

            try
            {
                conn = new SqlConnection(connStr);
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql_get_application, conn);
                cmd.Parameters.AddWithValue("@name", application);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    app_id = (int)reader["id"];
                }
                reader.Close();
                conn.Close();
                if (app_id == 0)
                    return -1;
                return app_id;
            }
            catch (Exception)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
                return -1;
            }
        }

        private int GetContainerID(string application)
        {
            string sql_get_container = "SELECT Id FROM Containers WHERE name = @name";
            SqlConnection conn = null;
            int app_id = 0;

            try
            {
                conn = new SqlConnection(connStr);
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql_get_container, conn);
                cmd.Parameters.AddWithValue("@name", application);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    app_id = (int)reader["id"];
                }
                reader.Close();
                conn.Close();
                if (app_id == 0)
                    return -1;
                return app_id;
            }
            catch (Exception)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
                return -1;
            }
        }

        private bool CheckFKViolation(SqlException ex)
        {
            // For illustration purposes, assuming that error number 547 indicates a foreign key violation
            return ex.Errors.Cast<SqlError>().Any(error => error.Number == 547);
        }
        //###################################################################################################################
        //APPLICATIONS ENDPOINTS

        // POST api/somiod/
        // Register new APP
        [HttpPost, Route("api/somiod")]
        public IHttpActionResult PostApplication()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.ContentType.ToLower().Contains("application/xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Application));
                    Application value = (Application)serializer.Deserialize(httpRequest.InputStream);

                    if (value.name != null &&value.name.Contains(" "))
                    {
                        return BadRequest("Invalid value, 'name' contains spaces");
                    }

                    string queryString = "INSERT INTO Applications VALUES (@name, @creation_dt); SELECT SCOPE_IDENTITY();";

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);

                        if (string.IsNullOrEmpty(value.name))
                        {
                            value.name = "Application";
                        }
                        value.name = EnsureNameisUnique(value.name, "Applications");
                        command.Parameters.AddWithValue("@name", value.name);
                        DateTime time = DateTime.Now;
                        string format = "yyyy-MM-dd HH:mm:ss";
                        value.creation_dt = time.ToString(format);
                        command.Parameters.AddWithValue("@creation_dt", value.creation_dt);

                        command.Connection.Open();
                        var lastId = command.ExecuteScalar();

                        if (lastId != null)
                        {
                            int id = Convert.ToInt32(lastId);
                            value.Id = id;
                            return Ok($"Application '{value.name}' added successfully");
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid content type. Expected application/xml.");
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // GET api/somiod/{application}
        // Get single APP info
        // Get all APP container names with -H somiod-discover: container
        [HttpGet, Route("api/somiod/{application}")]
        public IHttpActionResult GetSingleApplicationOrContainers(string application)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            if (Request.Headers.Contains("somiod-discover") &&
                Request.Headers.GetValues("somiod-discover").FirstOrDefault() == "container")
            {
                List<string> listOfContainers = new List<string>();

                string queryString = "SELECT * FROM Containers WHERE parent = @app_id";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("@app_id", app_id);

                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            string appName = (string)reader["name"];
                            listOfContainers.Add(appName);
                        }
                        reader.Close();
                    }

                    return Ok(listOfContainers);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }
            else
            {
                List<Application> listOfApplications = new List<Application>();

                string queryString = "SELECT TOP 1 * FROM Applications WHERE name = @name";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("@name", application);
                        command.Connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Application p = new Application
                                {
                                    Id = (int)reader["id"],
                                    name = (string)reader["name"],
                                    creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                };
                                listOfApplications.Add(p);
                            }
                        }
                    }
                    return Ok(listOfApplications);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }
        }

        // GET api/somiod
        // Get All APPs
        // Get List of APP names with -H somiod-discover: application
        [HttpGet, Route("api/somiod")]
        public IHttpActionResult GetApplications()
        {
            if (Request.Headers.Contains("somiod-discover") &&
                Request.Headers.GetValues("somiod-discover").FirstOrDefault() == "application")
            {
                List<string> listOfAppNames = new List<string>();

                string queryString = "SELECT [name] FROM Applications";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string appName = (string)reader["name"];
                                listOfAppNames.Add(appName);
                            }
                        }
                    }
                    return Ok(listOfAppNames);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }
            else
            {
                List<Application> listOfApplications = new List<Application>();

                string queryString = "SELECT * FROM Applications";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Application p = new Application
                                {
                                    Id = (int)reader["id"],
                                    name = (string)reader["name"],
                                    creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                };

                                listOfApplications.Add(p);
                            }
                        }
                    }
                    return Ok(listOfApplications);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }
        }

        // Put api/somiod/Applications
        // Update APP
        [HttpPut, Route("api/somiod/{application}")]
        public IHttpActionResult PutApplication(string application)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.ContentType.ToLower().Contains("application/xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Application));
                    Application value = (Application)serializer.Deserialize(httpRequest.InputStream);
                    
                    if (value.name.Contains(" "))
                    {
                        return BadRequest("Invalid value, 'name' contains spaces");
                    }

                    string queryString = "UPDATE Applications SET name = @name, creation_dt = @creation_dt WHERE name = @originalName";

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);

                        if (string.IsNullOrEmpty(value.name))
                        {
                            value.name = "Application";
                        }
                        value.name = EnsureNameisUnique(value.name, "Applications");
                        command.Parameters.AddWithValue("@name", value.name);

                        DateTime time = DateTime.Now;
                        string format = "yyyy-MM-dd HH:mm:ss";
                        string creationDate = time.ToString(format);
                        command.Parameters.AddWithValue("@creation_dt", creationDate);
                        command.Parameters.AddWithValue("@originalName", application);

                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok($"Application '{value.name}' updated successfully");
                        }
                        else
                        {
                            return NotFound();
                        }

                    }
                }
                else
                {
                    return BadRequest("Invalid content type. Expected application/xml.");
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // Delete api/somiod/Applications
        // Delete APP
        [HttpDelete, Route("api/somiod/{application}")]
        public IHttpActionResult DeleteApplication(string application)
        {
            string queryString = "DELETE FROM Applications WHERE name = @application";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@application", application);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok($"Application '{application}' deleted successfully");
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (SqlException e)
            {
                if (CheckFKViolation(e))
                {
                    return BadRequest("Unable to delete Application, all of its children need to be deleted first");
                }

                return InternalServerError();
            }
        }

        //###################################################################################################################
        //CONTAINER ENDPOINTS

        // POST api/somiod/application
        // Create CONTAINER
        [HttpPost, Route("api/somiod/{application}")]
        public IHttpActionResult PostContainer(string application)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.ContentType.ToLower().Contains("application/xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Container));
                    Container value = (Container)serializer.Deserialize(httpRequest.InputStream);

                    if (value.name != null && value.name.Contains(" "))
                    {
                        return BadRequest("Invalid value, 'name' contains spaces");
                    }

                    string queryString = "INSERT INTO Containers VALUES (@name, @creation_dt, @parent); SELECT SCOPE_IDENTITY();";

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);

                        if (string.IsNullOrEmpty(value.name))
                        {
                            value.name = "Container";
                        }
                        value.name = EnsureNameisUnique(value.name, "Containers");
                        command.Parameters.AddWithValue("@name", value.name);

                        DateTime time = DateTime.Now;
                        string format = "yyyy-MM-dd HH:mm:ss";
                        value.creation_dt = time.ToString(format);
                        command.Parameters.AddWithValue("@creation_dt", value.creation_dt);

                        int app_id = GetAppID(application);
                        if (app_id == -1)
                            return NotFound();
                        command.Parameters.AddWithValue("@parent", app_id);

                        try
                        {
                            command.Connection.Open();
                            var lastId = command.ExecuteScalar();

                            if (lastId != null)
                            {
                                int id = Convert.ToInt32(lastId);
                                value.Id = id;
                                value.parent = app_id;
                                return Ok($"Container '{value.name}' added successfully");
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                        catch (SqlException)
                        {
                            return InternalServerError();
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid content type. Expected application/xml.");
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // GET api/somiod/application/container
        // Get CONTAINER info
        // Get DATA names with -H somiod-discover: data
        // GET SUBSCRIPTIONS names with -H somiod-discover: subscriptions
        [HttpGet, Route("api/somiod/{application}/{container}")]
        public IHttpActionResult GetContainerValues(string application, string container)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            if (Request.Headers.Contains("somiod-discover") && Request.Headers.GetValues("somiod-discover").FirstOrDefault() == "data")
            {
                List<string> listOfData = new List<string>();

                string queryString = "SELECT * FROM Data WHERE parent = @container";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("@container", container_id);
                        command.Connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string p = (string)reader["name"];
                                listOfData.Add(p);
                            }
                        }
                    }
                    return Ok(listOfData);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }
            else if (Request.Headers.Contains("somiod-discover") && Request.Headers.GetValues("somiod-discover").FirstOrDefault() == "subscriptions")
            {
                List<string> listOfSubscriptions = new List<string>();

                string queryString = "SELECT * FROM Subscriptions WHERE parent = @container";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("@container", container_id);
                        command.Connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string p = (string)reader["name"];
                                listOfSubscriptions.Add(p);
                            }
                        }
                    }
                    return Ok(listOfSubscriptions);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }
            else
            {
                List<Container> listOfContainers = new List<Container>();

                string queryString = "SELECT TOP 1 * FROM Containers WHERE  Id = @id AND parent = @app";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);
                        command.Parameters.AddWithValue("@id", container_id);
                        command.Parameters.AddWithValue("@app", app_id);
                        command.Connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Container p = new Container
                                {
                                    Id = (int)reader["Id"],
                                    name = (string)reader["name"],
                                    creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                    parent = (int)reader["parent"],
                                };
                                listOfContainers.Add(p);
                            }
                        }
                    }
                    return Ok(listOfContainers);
                }
                catch (SqlException)
                {
                    return InternalServerError();
                }
            }

        }

        // GET api/somiod/application/container
        // Get Containers info of Application
        [HttpGet, Route("api/somiod/{application}/containers")]
        public IHttpActionResult GetApplicationContainers(string application)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            List<Container> listOfContainers = new List<Container>();

            string queryString = "SELECT * FROM Containers WHERE parent = @app";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@app", app_id);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Container p = new Container
                            {
                                Id = (int)reader["Id"],
                                name = (string)reader["name"],
                                creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                parent = (int)reader["parent"],
                            };
                            listOfContainers.Add(p);
                        }
                    }
                }
                return Ok(listOfContainers);
            }
            catch (SqlException)
            {
                return InternalServerError();
            }
        }

        //PUT api/somiod/Application/container
        // Update CONTAINER
        [HttpPut, Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PutContainer(string application, string container)
        {
            try
            {
                int app_id = GetAppID(application);
                if (app_id == -1)
                    return NotFound();

                int container_id = GetContainerID(container);
                if (container_id == -1)
                    return NotFound();

                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.ContentType.ToLower().Contains("application/xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Container));
                    Container value = (Container)serializer.Deserialize(httpRequest.InputStream);

                    if (value.name.Contains(" "))
                    {
                        return BadRequest("Invalid value, 'name' contains spaces");
                    }

                    string queryString = "UPDATE Containers SET name=@name WHERE Id=@id AND parent = @parent";

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        SqlCommand command = new SqlCommand(queryString, connection);

                        if (string.IsNullOrEmpty(value.name))
                        {
                            value.name = "Container";
                        }
                        value.name = EnsureNameisUnique(value.name, "Containers");
                        command.Parameters.AddWithValue("@name", value.name);
                        command.Parameters.AddWithValue("@id", container_id);
                        command.Parameters.AddWithValue("@parent", app_id);

                        try
                        {
                            command.Connection.Open();
                            int rows = command.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                return Ok($"Container '{value.name}' updated successfully");
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                        catch (SqlException)
                        {
                            return InternalServerError();
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid content type. Expected application/xml.");
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        //###################################################################################################################
        //CONTAINER DATA ENDPOINTS

        // GET api/somiod/{application}/{container}/data
        // Get single DATA
        [HttpGet, Route("api/somiod/{application}/{container}/data/{data}")]
        public IHttpActionResult GetSingleData(string application, string container, string data)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            List<ContainerData> listOfData= new List<ContainerData>();

            string queryString = "SELECT TOP 1 * FROM Data WHERE  name = @name AND parent = @container";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@name", data);
                    command.Parameters.AddWithValue("@container", container_id);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ContainerData p = new ContainerData
                            {
                                Id = (int)reader["Id"],
                                name = (string)reader["name"],
                                content = (string)reader["content"],
                                creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                parent = (int)reader["parent"],
                            };
                            listOfData.Add(p);
                        }
                    }
                }
                if (listOfData.Count > 0)
                    return Ok(listOfData);
                return NotFound();
            }
            catch (SqlException)
            {
                return InternalServerError();
            }
        }

        // GET api/somiod/{application}/{container}/data
        // Get ALL DATA from CONTAINER
        [HttpGet, Route("api/somiod/{application}/{container}/data")]
        public IHttpActionResult GetDataOfContainer(string application, string container)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            List<ContainerData> listOfData = new List<ContainerData>();

            string queryString = "SELECT * FROM Data WHERE parent = @container";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@container", container_id);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ContainerData p = new ContainerData
                            {
                                Id = (int)reader["Id"],
                                name = (string)reader["name"],
                                content = (string)reader["content"],
                                creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                parent = (int)reader["parent"],
                            };
                            listOfData.Add(p);
                        }
                    }
                }
                return Ok(listOfData);
            }
            catch (SqlException)
            {
                return InternalServerError();
            }
        }

        //###################################################################################################################
        //CONTAINER SUBSCRIPTION ENDPOINT

        // GET api/somiod/{application}/{container}/subscriptions/{subscriptions}
        // Get single SUBSCRIPTION
        [HttpGet, Route("api/somiod/{application}/{container}/subscriptions/{subscription}")]
        public IHttpActionResult GetSingleSubscription(string application, string container, string subscription)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            List<ContainerSubscriptions> listOfSubscriptions = new List<ContainerSubscriptions>();

            string queryString = "SELECT TOP 1 * FROM Subscriptions WHERE  name = @name AND parent = @container";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@name", subscription);
                    command.Parameters.AddWithValue("@container", container_id);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ContainerSubscriptions p = new ContainerSubscriptions
                            {
                                Id = (int)reader["Id"],
                                name = (string)reader["name"],
                                creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                parent = (int)reader["parent"],
                                event_type = (string)reader["event"],
                                endpoint = (string)reader["endpoint"],
                            };
                            listOfSubscriptions.Add(p);
                        }
                    }
                }
                if (listOfSubscriptions.Count > 0)
                    return Ok(listOfSubscriptions);
                return NotFound();
            }
            catch (SqlException)
            {
                return InternalServerError();
            }
        }

        // GET api/somiod/{application}/{container}/subscriptions
        // Get Subscriptions
        [HttpGet, Route("api/somiod/{application}/{container}/subscriptions")]
        public IHttpActionResult GetSubscriptionsOfContainer(string application, string container)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            List<ContainerSubscriptions> listOfSubscriptions = new List<ContainerSubscriptions>();

            string queryString = "SELECT * FROM Subscriptions WHERE parent = @container";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@container", container_id);
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ContainerSubscriptions p = new ContainerSubscriptions
                            {
                                Id = (int)reader["Id"],
                                name = (string)reader["name"],
                                creation_dt = ((DateTime)reader["creation_dt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                parent = (int)reader["parent"],
                                event_type = (string)reader["event"],
                                endpoint = (string)reader["endpoint"],
                            };
                            listOfSubscriptions.Add(p);
                        }
                    }
                }
                return Ok(listOfSubscriptions);
            }
            catch (SqlException)
            {
                return InternalServerError();
            }
        }

        //###################################################################################################################
        //DATA AND SUBSCRIPTIONS ENDPOINTS

        // POST api/somiod/{application}/{container}
        // Create DATA
        // Create SUBSCRIPTION
        [HttpPost, Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PostDataOrSubscription(string application, string container)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.ContentType.ToLower().Contains("application/xml"))
                {

                    XDocument xmlData = XDocument.Load(httpRequest.InputStream);
                    string resType = xmlData.Element("Request").Element("res_type")?.Value;

                    if (resType == "data")
                    {
                        XElement xmlContent = xmlData.Element("Request").Element("xmlData").Element("ContainerData");
                        XmlSerializer serializer = new XmlSerializer(typeof(ContainerData));

                        using (XmlReader reader = xmlContent.CreateReader())
                        {
                            ContainerData value = (ContainerData)serializer.Deserialize(reader);

                            if (value == null)
                                return NotFound();

                            if (value.name != null && value.name.Contains(" "))
                                return BadRequest("Invalid value, field name contains spaces");

                            if (value.content == null)
                                return BadRequest("Invalid content, field content is required");

                            string queryString = "INSERT INTO Data (name, content, creation_dt, parent) VALUES (@name, @content, @creation_dt, @parent); SELECT SCOPE_IDENTITY();";

                            using (SqlConnection connection = new SqlConnection(connStr))
                            {
                                SqlCommand command = new SqlCommand(queryString, connection);
                                if (string.IsNullOrEmpty(value.name))
                                {
                                    value.name = "DataContainer";
                                }
                                value.name = EnsureNameisUnique(value.name, "Data");
                                command.Parameters.AddWithValue("@name", value.name);

                                command.Parameters.AddWithValue("@content", value.content);

                                DateTime time = DateTime.Now;
                                string format = "yyyy-MM-dd HH:mm:ss";
                                value.creation_dt = time.ToString(format);
                                command.Parameters.AddWithValue("@creation_dt", value.creation_dt);

                                command.Parameters.AddWithValue("@parent", container_id);

                                command.Connection.Open();
                                var lastId = command.ExecuteScalar();

                                if (lastId != null)
                                {
                                    int id = Convert.ToInt32(lastId);
                                    value.parent = container_id;
                                    value.Id = id;

                                    SendNotifications("Create", container_id, value.content);

                                    return Ok($"Data '{value.name}' was inserted with success");
                                }
                                else
                                {
                                    return NotFound();
                                }

                            }
                        }

                    }
                    else if (resType == "subscription")
                    {
                        XElement xmlContent = xmlData.Element("Request").Element("xmlData").Element("ContainerSubscriptions");
                        XmlSerializer serializer = new XmlSerializer(typeof(ContainerSubscriptions));

                        using (XmlReader reader = xmlContent.CreateReader())
                        {
                            ContainerSubscriptions value = (ContainerSubscriptions)serializer.Deserialize(reader);

                            if (value == null)
                                return NotFound();

                            if (value.endpoint == null)
                                return BadRequest("Invalid content, field endpoint is required");

                            if (value.event_type == null)
                                return BadRequest("Invalid content, field event_type is required");

                            if (value.name != null && value.name.Contains(" "))
                                return BadRequest("Invalid value, field name contains spaces");
                            
                            if (!(value.event_type.Equals("CREATE") || value.event_type.Equals("DELETE")|| value.event_type.Equals("BOTH")))
                                return BadRequest("Error: Subscription event must be CREATE, DELETE or BOTH");

                            if (!(value.endpoint.StartsWith("mqtt://") || value.endpoint.StartsWith("http://") || value.endpoint.StartsWith("https://")))
                                return BadRequest("Error: Endpoint must start with 'mqtt://', 'http://' or 'https://'");

                            string queryString = "INSERT INTO Subscriptions VALUES (@name, @creation_dt, @parent, @event, @endpoint); SELECT SCOPE_IDENTITY();";

                            using (SqlConnection connection = new SqlConnection(connStr))
                            {
                                SqlCommand command = new SqlCommand(queryString, connection);

                                if (string.IsNullOrEmpty(value.name))
                                {
                                    value.name = "SubscriptionContainer";
                                }
                                value.name = EnsureNameisUnique(value.name, "Subscriptions");
                                command.Parameters.AddWithValue("@name", value.name);
                                command.Parameters.AddWithValue("@event", value.event_type);
                                command.Parameters.AddWithValue("@endpoint", value.endpoint);
                                command.Parameters.AddWithValue("@parent", container_id);

                                DateTime time = DateTime.Now;
                                string format = "yyyy-MM-dd HH:mm:ss";
                                value.creation_dt = time.ToString(format);
                                command.Parameters.AddWithValue("@creation_dt", value.creation_dt);

                                try
                                {
                                    command.Connection.Open();
                                    var lastId = command.ExecuteScalar();

                                    if (lastId != null)
                                    {
                                        int id = Convert.ToInt32(lastId);
                                        value.Id = id;
                                        value.parent = app_id;
                                        return Ok($"Subscription '{value.name}' was inserted with success");
                                    }
                                    else
                                    {
                                        return NotFound();
                                    }
                                }
                                catch (SqlException)
                                {
                                    return InternalServerError();
                                }
                            }
                        }
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                else
                {
                    return BadRequest("Invalid content type. Expected application/xml.");
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // DELETE api/somiod/{application}/{container}
        // Delete CONTAINER
        // Delete DATA
        // Delete SUBSCRIPTION
        [HttpDelete, Route("api/somiod/{application}/{container}")]
        public IHttpActionResult DeleteContainerAndInfo(string application, string container)
        {
            int app_id = GetAppID(application);
            if (app_id == -1)
                return NotFound();

            int container_id = GetContainerID(container);
            if (container_id == -1)
                return NotFound();

            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.ContentType.ToLower().Contains("application/xml"))
                {

                    XDocument xmlData = XDocument.Load(httpRequest.InputStream);
                    string resType = xmlData.Element("Request").Element("res_type")?.Value;


                    if (resType == "container")
                    {
                        XElement xmlContent = xmlData.Element("Request").Element("xmlData").Element("Container");
                        XmlSerializer serializer = new XmlSerializer(typeof(Container));

                        using (XmlReader reader = xmlContent.CreateReader())
                        {
                            Container value = (Container)serializer.Deserialize(reader);
                            string queryString = "DELETE Containers WHERE name=@container AND parent = @parent";

                            using (SqlConnection connection = new SqlConnection(connStr))
                            {
                                SqlCommand command = new SqlCommand(queryString, connection);
                                command.Parameters.AddWithValue("@container", value.name);
                                command.Parameters.AddWithValue("@parent", app_id);

                                try
                                {
                                    command.Connection.Open();
                                    int rows = command.ExecuteNonQuery();
                                    if (rows > 0)
                                    {
                                        return Ok($"Container '{value.name}' deleted successfully");
                                    }
                                    else
                                    {
                                        return NotFound();
                                    }
                                }
                                catch (SqlException e)
                                {
                                    if (CheckFKViolation(e))
                                    {
                                        return BadRequest("Unable to delete Container, all of its children need to be deleted first");
                                    }

                                    return InternalServerError();
                                }
                            }
                        }
                    }
                    else if (resType == "data")
                    {

                        XElement xmlContent = xmlData.Element("Request").Element("xmlData").Element("ContainerData");
                        XmlSerializer serializer = new XmlSerializer(typeof(ContainerData));

                        using (XmlReader reader = xmlContent.CreateReader())
                        {
                            ContainerData value = (ContainerData)serializer.Deserialize(reader);

                            if (value == null)
                            {
                                return NotFound();
                            }  

                            string queryString = "DELETE FROM Data WHERE name = @container AND parent = @parent";

                            using (SqlConnection connection = new SqlConnection(connStr))
                            {
                                SqlCommand command = new SqlCommand(queryString, connection);
                                command.Parameters.AddWithValue("@container", value.name);
                                command.Parameters.AddWithValue("@parent", container_id);

                                try
                                {
                                    connection.Open();
                                    int rowsAffected = command.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        SendNotifications("Delete", container_id, "");

                                        return Ok($"Container Data '{container}' deleted successfully");
                                    }
                                    else
                                    {
                                        return NotFound();
                                    }
                                }
                                catch (SqlException)
                                {
                                    return InternalServerError();
                                }
                            }

                        }
                    }
                    else if (resType == "subscription")
                    {
                        XElement xmlContent = xmlData.Element("Request").Element("xmlData").Element("ContainerSubscriptions");
                        XmlSerializer serializer = new XmlSerializer(typeof(ContainerSubscriptions));

                        using (XmlReader reader = xmlContent.CreateReader())
                        {
                            ContainerSubscriptions value = (ContainerSubscriptions)serializer.Deserialize(reader);

                            if (value == null)
                            {
                                return NotFound();
                            }
                            string queryString = "DELETE Subscriptions WHERE name=@name AND parent = @parent";

                            using (SqlConnection connection = new SqlConnection(connStr))
                            {
                                SqlCommand command = new SqlCommand(queryString, connection);
                                command.Parameters.AddWithValue("@name", value.name);
                                command.Parameters.AddWithValue("@parent", container_id);

                                try
                                {
                                    command.Connection.Open();
                                    int rows = command.ExecuteNonQuery();
                                    if (rows > 0)
                                    {
                                        return Ok($"Container Subscription '{value.name}' deleted successfully");
                                    }
                                    else
                                    {
                                        return NotFound();
                                    }
                                }
                                catch (SqlException)
                                {
                                    return InternalServerError();
                                }
                            }
                        }
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                else
                {
                    return BadRequest("Invalid content type. Expected application/xml.");
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        //###########################################################################################################
        // Notification & Name Handling

        private void SendNotifications(string eventType, int containerId, string content)
        {
            string queryString = "SELECT * FROM Subscriptions WHERE parent = @containerId AND ([event] = @eventType OR [event] = 'BOTH')";

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@containerId", containerId);
                    command.Parameters.AddWithValue("@eventType", eventType);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        string endpoint;
                        while (reader.Read()) 
                        {
                            endpoint = (string)reader["endpoint"];
                            if (endpoint.StartsWith("mqtt://"))
                            {
                                string subscription = (string)reader["name"];
                                mqttService.ConnectAndPublish(subscription, content);
                            }
                            else if (endpoint.StartsWith("http://") || endpoint.StartsWith("https://"))
                            {
                            
                                using (HttpClient httpClient = new HttpClient())
                                {
                                    string url = $"{(string)reader["endpoint"]}{(string)reader["name"]}";

                                    HttpContent httpContent = new StringContent(content, Encoding.UTF8, "application/xml");

                                    HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error Sending Notifications", e);
            }
        }

        //NOTE: Ensures New Names are all Unique (name: Name to check, resourceType: DB Table Name)
        private string EnsureNameisUnique(string name, string resourceType)
        {
            try
            {
                string queryString = $"SELECT * FROM {resourceType} WHERE name = @name";

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    SqlCommand command = new SqlCommand(queryString, connection);
                    command.Parameters.AddWithValue("@name", name);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        string newName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}";
                        return newName;
                    }
                    else
                    {
                        return name;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}