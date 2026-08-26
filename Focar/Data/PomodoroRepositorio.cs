using Focar.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Focar.Data
{
	class PomodoroRepositorio
	{
		private readonly string _connectionString;

		public PomodoroRepositorio()
		{
			var pastaDados = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Focar");

			Directory.CreateDirectory(pastaDados);

			var dbPath = Path.Combine(pastaDados, "pomodoro.db");
			_connectionString = $"Data Source={dbPath}";

			Inicializar();
		}

		private void Inicializar()
		{
			using var conn = new SqliteConnection(_connectionString);
			conn.Open();

			var cmd = conn.CreateCommand();
			cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PomodoroSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Tipo INTEGER NOT NULL,
                    InicioEm TEXT NOT NULL,
                    FimEm TEXT NULL,
                    DuracaoMinutos INTEGER NOT NULL,
                    Concluida INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PomodoroConfig (
                    Chave TEXT PRIMARY KEY,
                    Valor TEXT NOT NULL
                );
            ";
			cmd.ExecuteNonQuery();
		}

		public async Task<int> IniciarSessaoAsync(PomodoroSessao sessao)
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = @"
                INSERT INTO PomodoroSessions (Tipo, InicioEm, FimEm, DuracaoMinutos, Concluida)
                VALUES ($tipo, $inicio, NULL, $duracao, 0);
                SELECT last_insert_rowid();
            ";
			cmd.Parameters.AddWithValue("$tipo", (int)sessao.Tipo);
			cmd.Parameters.AddWithValue("$inicio", sessao.InicioEm.ToString("O"));
			cmd.Parameters.AddWithValue("$duracao", sessao.DuracaoMinutos);

			var result = await cmd.ExecuteScalarAsync();
			return Convert.ToInt32(result);
		}

		public async Task FinalizarSessaoAsync(int id, bool concluida)
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = @"
                UPDATE PomodoroSessions
                SET FimEm = $fim, Concluida = $concluida
                WHERE Id = $id;
            ";
			cmd.Parameters.AddWithValue("$fim", DateTime.Now.ToString("O"));
			cmd.Parameters.AddWithValue("$concluida", concluida ? 1 : 0);
			cmd.Parameters.AddWithValue("$id", id);

			await cmd.ExecuteNonQueryAsync();
		}

		public async Task<int> ContarPomodorosConcluidosHojeAsync()
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = @"
                SELECT COUNT(*) FROM PomodoroSessions
                WHERE Tipo = $tipo
                  AND Concluida = 1
                  AND date(InicioEm) = date('now', 'localtime');
            ";
			cmd.Parameters.AddWithValue("$tipo", (int)TipoSessao.Foco);

			var result = await cmd.ExecuteScalarAsync();
			return Convert.ToInt32(result);
		}

		public async Task<string?> ObterConfigAsync(string chave)
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = "SELECT Valor FROM PomodoroConfig WHERE Chave = $chave;";
			cmd.Parameters.AddWithValue("$chave", chave);

			var result = await cmd.ExecuteScalarAsync();
			return result?.ToString();
		}

		public async Task SalvarConfigAsync(string chave, string valor)
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = @"
                INSERT INTO PomodoroConfig (Chave, Valor) VALUES ($chave, $valor)
                ON CONFLICT(Chave) DO UPDATE SET Valor = excluded.Valor;
            ";
			cmd.Parameters.AddWithValue("$chave", chave);
			cmd.Parameters.AddWithValue("$valor", valor);

			await cmd.ExecuteNonQueryAsync();
		}

		public async Task ExcluirTodosRegistrosAsync()
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = "DELETE FROM PomodoroSessions;";

			await cmd.ExecuteNonQueryAsync();
		}

		public async Task ExcluirRegistroAsync(int id)
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var cmd = conn.CreateCommand();
			cmd.CommandText = "DELETE FROM PomodoroSessions WHERE Id = $id;";
			cmd.Parameters.AddWithValue("$id", id);

			await cmd.ExecuteNonQueryAsync();
		}

		public async Task<List<PomodoroRegistroExibicao>> ListarRegistrosAsync(
			DateTime? dataInicio = null,
			DateTime? dataFim = null,
			TipoSessao? tipo = null)
		{
			using var conn = new SqliteConnection(_connectionString);
			await conn.OpenAsync();

			var sql = new StringBuilder(@"
                SELECT Id, Tipo, InicioEm, FimEm, DuracaoMinutos, Concluida
                FROM PomodoroSessions
                WHERE 1 = 1
            ");

			if (dataInicio is not null)
				sql.Append(" AND date(InicioEm) >= date($dataInicio) ");

			if (dataFim is not null)
				sql.Append(" AND date(InicioEm) <= date($dataFim) ");

			if (tipo is not null)
				sql.Append(" AND Tipo = $tipo ");

			sql.Append(" ORDER BY InicioEm DESC;");

			var cmd = conn.CreateCommand();
			cmd.CommandText = sql.ToString();

			if (dataInicio is not null)
				cmd.Parameters.AddWithValue("$dataInicio", dataInicio.Value.ToString("yyyy-MM-dd"));

			if (dataFim is not null)
				cmd.Parameters.AddWithValue("$dataFim", dataFim.Value.ToString("yyyy-MM-dd"));

			if (tipo is not null)
				cmd.Parameters.AddWithValue("$tipo", (int)tipo.Value);

			var lista = new List<PomodoroRegistroExibicao>();

			using var reader = await cmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var inicio = DateTime.Parse(reader.GetString(2));
				DateTime? fim = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3));

				lista.Add(new PomodoroRegistroExibicao
				{
					Id = reader.GetInt32(0),
					Tipo = (TipoSessao)reader.GetInt32(1),
					InicioEm = inicio,
					FimEm = fim,
					DuracaoMinutos = reader.GetInt32(4),
					Concluida = reader.GetInt32(5) == 1
				});
			}

			return lista;
		}
	}
}
