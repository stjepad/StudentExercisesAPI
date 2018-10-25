using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StudentExercisesAPI.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace StudentExercisesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CohortsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CohortsController(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        
        //localhost:5000/api/Cohorts? q = 12
        [HttpGet]
        public async Task<IActionResult> Get(string q)
        {
            string sql = @"
            SELECT
                c.Id,
                c.Name
            FROM Cohort c
            WHERE 1=1
            ";

            if (q != null)
            {
                string isQ = $@"
                    AND c.Name LIKE '%{q}%'
                ";
                sql = $"{sql} {isQ}";
            }

            Console.WriteLine(sql);

            using (IDbConnection conn = Connection)
            {

                IEnumerable<Cohort> cohorts = await conn.QueryAsync<Cohort>
                    (
                    sql
                    );
                return Ok(cohorts);
            }
        }

        // GET api/cohorts/5
        [HttpGet("{id}", Name = "GetCohort")]
        public async Task<IActionResult> Get([FromRoute]int id)
        {
            string sql = $@"
            SELECT
                c.Id,
                c.Name
            FROM Cohort c
            WHERE c.Id = {id}
            ";

            using (IDbConnection conn = Connection)
            {
                IEnumerable<Cohort> cohorts = await conn.QueryAsync<Cohort>(sql);
                return Ok(cohorts);
            }
        }

        // POST api/cohorts
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Cohort cohort)
        {
            string sql = $@"INSERT INTO Cohort 
            (Name)
            VALUES
            (
                '{cohort.Name}'
            );
            SELECT SCOPE_IDENTITY();";

            using (IDbConnection conn = Connection)
            {
                var newId = (await conn.QueryAsync<int>(sql)).Single();
                cohort.Id = newId;
                return CreatedAtRoute("GetStudent", new { id = newId }, cohort);
            }
        }

        // PUT api/cohorts/5
        // PUT method needs all information in order to not get 404, but with changes expressed. 
        //Ex: [{"id":5,"name":"Day Cohort 13","students":[],"instructors":[]}]

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Cohort cohort)
        {
            string sql = $@"
            UPDATE Cohort
            SET Name = '{cohort.Name}'

            WHERE Id = {id}";

            try
            {
                using (IDbConnection conn = Connection)
                {
                    int rowsAffected = await conn.ExecuteAsync(sql);
                    if (rowsAffected > 0)
                    {
                        return new StatusCodeResult(StatusCodes.Status204NoContent);
                    }
                    throw new Exception("No rows affected");
                }
            }
            catch (Exception)
            {
                if (!CohortExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE api/cohort/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string sql = $@"DELETE FROM Cohort WHERE Id = {id}
                        DROP FOREIGN KEY FK_Cohort;";

            using (IDbConnection conn = Connection)
            {
                int rowsAffected = await conn.ExecuteAsync(sql);
                if (rowsAffected > 0)
                {
                    return new StatusCodeResult(StatusCodes.Status204NoContent);
                }
                throw new Exception("No rows affected");
            }

        }

        private bool CohortExists(int id)
        {
            string sql = $"SELECT Id FROM Student WHERE Id = {id}";
            using (IDbConnection conn = Connection)
            {
                return conn.Query<Cohort>(sql).Count() > 0;
            }
        }

    }
}