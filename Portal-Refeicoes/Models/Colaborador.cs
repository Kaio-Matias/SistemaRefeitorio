using System;



namespace Portal_Refeicoes.Models;

public class Colaborador
{
public int Id { get; set; }
public string CartaoPonto { get; set; }
public string Nome { get; set; }
public byte[]? Foto { get; set; }
public string? AzureId { get; set; }
public int DepartamentoId { get; set; }
public Departamento Departamento { get; set; }
public int FuncaoId { get; set; }
public Funcao Funcao { get; set; }
}