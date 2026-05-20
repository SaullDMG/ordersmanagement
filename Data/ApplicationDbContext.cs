using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrdersManagement.Models;
using OrdersManagement.Data;  

namespace OrdersManagement.Data
{

  
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }
        //Constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }


        //Propiedad para crear la tabla en la BD
        public virtual DbSet<Cliente> Clientes { get; set; }
        public virtual DbSet<Diagnostico> Diagnosticos { get; set; }
        public virtual DbSet<Equipo> Equipos { get; set; }
        public virtual DbSet<Evidencia> Evidencias { get; set; }
        public virtual DbSet<OrdenServicio> OrdenesServicio { get; set; }   
        public virtual DbSet<Usuario> Usuarios { get; set; }
    
    }
}