using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ordersmanagement.Models;
using OrdersManagement.Models;

namespace OrdersManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor principal que utiliza ASP.NET Core para inyectar la configuración
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Propiedades para crear las tablas en la BD
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Diagnostico> Diagnosticos { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Evidencia> Evidencias { get; set; }
        public DbSet<OrdenServicio> OrdenesServicio { get; set; }   
        public DbSet<Usuario> Usuarios { get; set; }

        // Aquí mantenemos solo los Índices (Indexes) personalizados, las llaves y autoincrementables se infieren por convención
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configuración de Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                // Índice único para evitar teléfonos duplicados y acelerar búsquedas
                entity.HasIndex(c => c.Telefono).IsUnique();
            });

            // 2. Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                // Índices únicos esenciales para autenticación y contacto
                entity.HasIndex(u => u.Correo).IsUnique();
                entity.HasIndex(u => u.Telefono).IsUnique();
            });

            // 3. Configuración de Equipo
            modelBuilder.Entity<Equipo>(entity =>
            {
                // Índice para optimizar búsquedas frecuentes por número de serie
                entity.HasIndex(e => e.Serie);
            });
            
            // Nota: OrdenServicio, Diagnostico y Evidencia ya no requieren configuración explícita aquí
            // porque sus llaves primarias se llaman 'Id' y EF Core las mapea solas.
        }
    }
}