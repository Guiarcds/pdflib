namespace OrganizadorDocumentos.Core.Services;

using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using ImageMagick;
using OrganizadorDocumentos.Core.Enums;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class FileService : IFileService
{
    private readonly INormalizacaoService _normalizacao;
    private readonly ILogService _log;

    public FileService(INormalizacaoService normalizacao, ILogService log)
    {
        _normalizacao = normalizacao;
        _log = log;
    }

    public bool PastaExiste(string caminho)
    {
        return Directory.Exists(caminho);
    }

    public bool ArquivoExiste(string caminho)
    {
        return File.Exists(caminho);
    }

    public string BuscarPastaAno(int ano, string caminhoColaborador)
    {
        if (!Directory.Exists(caminhoColaborador))
        {
            _log.Aviso($"Pasta do colaborador não encontrada: {caminhoColaborador}");
            throw new DirectoryNotFoundException($"Pasta não encontrada: {caminhoColaborador}");
        }

        var diretorios = Directory.GetDirectories(caminhoColaborador);
        var anoStr = ano.ToString();

        foreach (var dir in diretorios)
        {
            var nome = Path.GetFileName(dir);
            if (nome == anoStr)
            {
                _log.Debug($"Pasta ano encontrada (exata): {dir}");
                return dir;
            }
        }

        foreach (var dir in diretorios)
        {
            var nome = Path.GetFileName(dir);
            if (nome.StartsWith(anoStr) || Regex.IsMatch(nome, $"^{ano}[_\\s-]"))
            {
                _log.Debug($"Pasta ano encontrada (variação): {dir}");
                return dir;
            }
        }

        var novoCaminho = Path.Combine(caminhoColaborador, anoStr);
        Directory.CreateDirectory(novoCaminho);
        _log.Informacao($"Pasta ano criada: {novoCaminho}");
        return novoCaminho;
    }

    public string BuscarPastaMes(int mes, int ano, string caminhoAno)
    {
        if (!Directory.Exists(caminhoAno))
        {
            _log.Aviso($"Pasta do ano não encontrada: {caminhoAno}");
            throw new DirectoryNotFoundException($"Pasta não encontrada: {caminhoAno}");
        }

        var diretorios = Directory.GetDirectories(caminhoAno);
        var nomeMes = _normalizacao.NomeMesPorExtenso(mes);
        var nomesPossiveis = new[]
        {
            $"{mes:D2} - {nomeMes}",
            $"{mes:D2} {nomeMes}",
            $"{mes:D2}_{nomeMes}",
            nomeMes,
            mes.ToString("D2"),
            mes.ToString()
        };

        foreach (var dir in diretorios)
        {
            var nomePasta = Path.GetFileName(dir);
            var nomeNorm = _normalizacao.NormalizarNome(nomePasta);

            foreach (var possivel in nomesPossiveis)
            {
                if (nomeNorm == _normalizacao.NormalizarNome(possivel))
                {
                    _log.Debug($"Pasta mês encontrada: {dir}");
                    return dir;
                }
            }

            var match = Regex.Match(nomePasta, @"^(\d{1,2})");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var mesEncontrado) && mesEncontrado == mes)
            {
                _log.Debug($"Pasta mês encontrada (por número): {dir}");
                return dir;
            }
        }

        var novoNome = $"{mes:D2} - {nomeMes}";
        var novoCaminho = Path.Combine(caminhoAno, novoNome);
        Directory.CreateDirectory(novoCaminho);
        _log.Informacao($"Pasta mês criada: {novoCaminho}");
        return novoCaminho;
    }

    public void MoverArquivo(string origem, string destino, bool sobrescrever = false)
    {
        if (!File.Exists(origem))
            throw new FileNotFoundException($"Arquivo não encontrado: {origem}");

        var diretorioDestino = Path.GetDirectoryName(destino);
        if (!string.IsNullOrEmpty(diretorioDestino) && !Directory.Exists(diretorioDestino))
            Directory.CreateDirectory(diretorioDestino);

        if (File.Exists(destino))
        {
            if (sobrescrever)
            {
                File.Delete(destino);
                _log.Aviso($"Arquivo de destino sobrescrito: {destino}");
            }
            else
            {
                // Gera nome único automaticamente
                var nomeBase = Path.GetFileNameWithoutExtension(destino);
                var extensao = Path.GetExtension(destino);
                var caminhoUnico = NomeArquivoUnico(diretorioDestino!, nomeBase, extensao);
                destino = caminhoUnico;
                _log.Aviso($"Arquivo já existe, usando nome único: {Path.GetFileName(destino)}");
            }
        }

        File.Move(origem, destino);
        _log.Informacao($"Arquivo movido: {origem} -> {destino}");
    }

    public void CriarPasta(string caminho)
    {
        if (!Directory.Exists(caminho))
        {
            Directory.CreateDirectory(caminho);
            _log.Informacao($"Pasta criada: {caminho}");
        }
    }

    public List<string> ListarPdfs(string pasta)
    {
        if (!Directory.Exists(pasta))
        {
            _log.Aviso($"Pasta não encontrada: {pasta}");
            return new List<string>();
        }

        return Directory.GetFiles(pasta, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();
    }

    public string NomeArquivoUnico(string caminhoDestino, string nomeBase, string extensao)
    {
        var caminho = Path.Combine(caminhoDestino, $"{nomeBase}{extensao}");
        if (!File.Exists(caminho))
            return caminho;

        int contador = 1;
        do
        {
            caminho = Path.Combine(caminhoDestino, $"{nomeBase}_{contador}{extensao}");
            contador++;
        } while (File.Exists(caminho));

        return caminho;
    }

    public ResultadoMoverPasta MoverPastaCompleta(string origem, string destino)
    {
        if (string.IsNullOrWhiteSpace(origem) || string.IsNullOrWhiteSpace(destino))
            throw new ArgumentException("Origem e destino são obrigatórios.");

        origem = Path.GetFullPath(origem);
        destino = Path.GetFullPath(destino);

        if (string.Equals(origem, destino, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A pasta de origem e de destino não podem ser a mesma.");

        if (!Directory.Exists(origem))
            throw new DirectoryNotFoundException($"Pasta de origem não encontrada: {origem}");

        Directory.CreateDirectory(destino);

        var resultado = new ResultadoMoverPasta();

        foreach (var dirAno in Directory.GetDirectories(origem))
        {
            var ano = ExtrairAno(Path.GetFileName(dirAno));
            var dirAnoDestino = ano > 0
                ? BuscarPastaAno(ano, destino)
                : CriarPastaCorrespondente(destino, dirAno);

            foreach (var dirMes in Directory.GetDirectories(dirAno))
            {
                var mes = ExtrairMes(Path.GetFileName(dirMes));
                var jaExistia = mes > 0 && PastaMesExiste(dirAnoDestino, mes);

                var dirMesDestino = mes > 0
                    ? BuscarPastaMes(mes, ano, dirAnoDestino)
                    : CriarPastaCorrespondente(dirAnoDestino, dirMes);

                if (jaExistia)
                    resultado.PastasMesReutilizadas++;
                else
                    resultado.PastasMesCriadas++;

                MoverArquivosDaPasta(dirMes, dirMesDestino, resultado);
            }
        }

        if (resultado.Sucesso)
        {
            Directory.Delete(origem, true);
            _log.Informacao($"Pasta de origem removida após unificação: {origem}");
        }
        else
        {
            _log.Erro($"Unificação interrompida com {resultado.Erros.Count} erro(s). " +
                      $"A pasta de origem foi preservada: {origem}");
        }

        _log.Informacao($"Unificação concluída: {resultado.ArquivosMovidos} arquivo(s) movido(s), " +
                        $"{resultado.ConflitosRenomeados} renomeado(s) por conflito, " +
                        $"{resultado.PastasMesCriadas} pasta(s) de mês criada(s)");

        return resultado;
    }

    private string CriarPastaCorrespondente(string pastaPai, string pastaOrigem)
    {
        var caminho = Path.Combine(pastaPai, Path.GetFileName(pastaOrigem));
        Directory.CreateDirectory(caminho);
        _log.Informacao($"Pasta criada durante unificação: {caminho}");
        return caminho;
    }

    private bool PastaMesExiste(string caminhoAno, int mes)
    {
        var nomeMes = _normalizacao.NormalizarNome(_normalizacao.NomeMesPorExtenso(mes));

        return Directory.GetDirectories(caminhoAno).Any(dir =>
        {
            var nome = Path.GetFileName(dir);
            return Regex.IsMatch(nome, $"^{mes:D2}($|[^0-9])") ||
                   Regex.IsMatch(nome, $"^{mes}($|[^0-9])") ||
                   (!string.IsNullOrEmpty(nomeMes) && _normalizacao.NormalizarNome(nome).Contains(nomeMes));
        });
    }

    private void MoverArquivosDaPasta(string pastaOrigem, string pastaDestino, ResultadoMoverPasta resultado)
    {
        var arquivos = Directory.GetFiles(pastaOrigem, "*", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var arquivo in arquivos)
        {
            try
            {
                var relativo = Path.GetRelativePath(pastaOrigem, arquivo);
                var pastaDestinoFinal = Path.GetDirectoryName(Path.Combine(pastaDestino, relativo))!;
                Directory.CreateDirectory(pastaDestinoFinal);

                var nomeBase = Path.GetFileNameWithoutExtension(arquivo);
                var extensao = Path.GetExtension(arquivo);
                var destino = NomeArquivoUnico(pastaDestinoFinal, nomeBase, extensao);
                var renomeado = !string.Equals(
                    Path.GetFileName(destino),
                    Path.GetFileName(arquivo),
                    StringComparison.OrdinalIgnoreCase);

                File.Move(arquivo, destino);
                resultado.ArquivosMovidos++;

                if (renomeado)
                {
                    resultado.ConflitosRenomeados++;
                    _log.Aviso($"Nome duplicado na unificação: '{Path.GetFileName(arquivo)}' -> '{Path.GetFileName(destino)}'");
                }
            }
            catch (Exception ex)
            {
                resultado.Erros.Add($"{arquivo}: {ex.Message}");
                _log.Erro($"Erro ao mover arquivo durante unificação: {arquivo}", ex);
            }
        }
    }

    private static int ExtrairAno(string nomePasta)
    {
        var match = Regex.Match(nomePasta, @"^(\d{4})");
        return match.Success && int.TryParse(match.Groups[1].Value, out var ano) ? ano : 0;
    }

    private int ExtrairMes(string nomePasta)
    {
        var match = Regex.Match(nomePasta, @"^(\d{1,2})");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var mes) && mes is >= 1 and <= 12)
            return mes;

        var nomeNormalizado = _normalizacao.NormalizarNome(nomePasta);
        for (var i = 1; i <= 12; i++)
        {
            var nomeMes = _normalizacao.NormalizarNome(_normalizacao.NomeMesPorExtenso(i));
            if (!string.IsNullOrEmpty(nomeMes) && nomeNormalizado.Contains(nomeMes))
                return i;
        }

        return 0;
    }

    public async Task<List<string>> DividirPdfAsync(string caminhoPdf, List<DocumentoFinanceiro> documentos, string pastaSaida)
    {
        var arquivosGerados = new List<string>();

        if (!File.Exists(caminhoPdf))
        {
            _log.Erro($"PDF não encontrado para divisão: {caminhoPdf}");
            return arquivosGerados;
        }

        if (documentos == null || documentos.Count == 0)
        {
            _log.Aviso("Nenhum documento para dividir");
            return arquivosGerados;
        }

        try
        {
            _log.Informacao($"Dividindo PDF '{Path.GetFileName(caminhoPdf)}' em {documentos.Count} documento(s)");

            // Copia o PDF original para a pasta de saída como base
            Directory.CreateDirectory(pastaSaida);

            // Usa iText7 para ler o número total de páginas
            int totalPaginas;
            using (var reader = new PdfReader(caminhoPdf))
            using (var pdfDoc = new PdfDocument(reader))
            {
                totalPaginas = pdfDoc.GetNumberOfPages();
            }

            // Se só tem 1 documento, apenas copia o arquivo
            if (documentos.Count == 1)
            {
                var doc = documentos[0];
                var nomeArquivo = GerarNomeArquivoParaDivisao(doc);
                var caminhoDestino = Path.Combine(pastaSaida, nomeArquivo);
                File.Copy(caminhoPdf, caminhoDestino, true);
                arquivosGerados.Add(caminhoDestino);
                _log.Informacao($"Documento único copiado: {caminhoDestino}");
                return arquivosGerados;
            }

            // Para múltiplos documentos, divide igualmente as páginas (aproximação)
            // Nota: Ideal seria que a IA retornasse intervalos de páginas, mas por enquanto divide igualmente
            int paginasPorDoc = Math.Max(1, totalPaginas / documentos.Count);

            for (int i = 0; i < documentos.Count; i++)
            {
                var doc = documentos[i];
                var inicio = i * paginasPorDoc + 1;
                var fim = (i == documentos.Count - 1) ? totalPaginas : (i + 1) * paginasPorDoc;

                if (inicio > totalPaginas) break;

                var nomeArquivo = GerarNomeArquivoParaDivisao(doc);
                var caminhoDestino = Path.Combine(pastaSaida, nomeArquivo);

                try
                {
                    using (var reader = new PdfReader(caminhoPdf))
                    using (var pdfDoc = new PdfDocument(reader))
                    using (var writer = new PdfWriter(caminhoDestino))
                    using (var newPdf = new PdfDocument(writer))
                    {
                        pdfDoc.CopyPagesTo(inicio, fim, newPdf);
                    }

                    if (File.Exists(caminhoDestino) && new FileInfo(caminhoDestino).Length > 0)
                    {
                        arquivosGerados.Add(caminhoDestino);
                        _log.Informacao($"Documento {i + 1}/{documentos.Count} salvo: {nomeArquivo} (páginas {inicio}-{fim})");
                    }
                    else
                    {
                        _log.Aviso($"Falha ao gerar documento {i + 1}: arquivo vazio ou não criado");
                    }
                }
                catch (Exception ex)
                {
                    _log.Erro($"Erro ao extrair páginas {inicio}-{fim} para documento {i + 1}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao dividir PDF: {caminhoPdf}", ex);
        }

        return arquivosGerados;
    }

    private string GerarNomeArquivoParaDivisao(DocumentoFinanceiro doc)
    {
        var sigla = doc.Sigla ?? "XX";
        var nomeColaborador = (doc.Colaborador ?? "DESCONHECIDO").Replace(" ", "_").Replace("/", "_");
        
        if (doc.Sigla == "OS" && !string.IsNullOrWhiteSpace(doc.NumeroOS))
        {
            return $"OS_{nomeColaborador}_OS-{doc.NumeroOS}.pdf";
        }

        var competencia = doc.Competencia != null && doc.Competencia.Completa
            ? $"{doc.Competencia.Mes:D2}-{doc.Competencia.Ano}"
            : "SEM_COMPETENCIA";

        return $"{sigla}_{nomeColaborador}_{competencia}.pdf";
    }

    public async Task<List<string>> AplicarCorrecoesAsync(string caminhoPdf, List<PageDecision> decisoes, string pastaTemp)
    {
        var imagensCorrigidas = new List<string>();

        if (!File.Exists(caminhoPdf))
        {
            _log.Erro($"PDF não encontrado para aplicação de correções: {caminhoPdf}");
            return imagensCorrigidas;
        }

        try
        {
            Directory.CreateDirectory(pastaTemp);

            var settings = new MagickReadSettings
            {
                Density = new Density(200, 200),
                Width = 1500
            };

            _log.Informacao($"Aplicando correções do PDF: {Path.GetFileName(caminhoPdf)}");

            using var images = new MagickImageCollection(caminhoPdf, settings);
            int totalPaginas = images.Count;

            var decisoesPorPagina = decisoes.ToDictionary(d => d.Page, d => d);

            for (int i = 0; i < totalPaginas; i++)
            {
                int pageNum = i + 1;

                if (!decisoesPorPagina.TryGetValue(pageNum, out var decisao))
                {
                    decisao = new PageDecision { Page = pageNum, Action = PageAction.Keep, Rotation = 0 };
                }

                var image = images[i];

                if (decisao.Action == PageAction.Discard)
                {
                    _log.Informacao($"Página {pageNum} descartada (motivo: {decisao.Reason ?? "não especificado"})");
                    continue;
                }

                image.AutoOrient();

                if (decisao.Action == PageAction.Rotate && decisao.Rotation != 0)
                {
                    _log.Informacao($"Página {pageNum} rotacionada {decisao.Rotation}°");
                    image.Rotate(decisao.Rotation);
                }

                image.Quality = 85;
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var nomeArquivo = $"corrected_p{pageNum}_{timestamp}.jpg";
                var caminhoImagem = Path.Combine(pastaTemp, nomeArquivo);
                image.Write(caminhoImagem);
                imagensCorrigidas.Add(caminhoImagem);

                _log.Debug($"Página {pageNum} corrigida salva: {nomeArquivo}");
            }

            _log.Informacao($"Correções aplicadas: {imagensCorrigidas.Count}/{totalPaginas} página(s) mantidas");
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao aplicar correções: {caminhoPdf}", ex);
            throw;
        }

        return imagensCorrigidas;
    }

    public async Task<List<string>> SplitUmaPaginaPorPdfAsync(List<string> imagensCorrigidas, string pastaTemp)
    {
        var pdfsGerados = new List<string>();
        var pastaSplit = Path.Combine(pastaTemp, "split");
        Directory.CreateDirectory(pastaSplit);

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        foreach (var imagem in imagensCorrigidas)
        {
            if (!File.Exists(imagem))
            {
                _log.Aviso($"Imagem corrigida não encontrada: {imagem}");
                continue;
            }

            var nomeArquivo = Path.GetFileName(imagem);
            var match = Regex.Match(nomeArquivo, @"p(\d+)");
            int page = match.Success ? int.Parse(match.Groups[1].Value) : 0;

            var nomeBase = $"REC_{page}_{timestamp}";
            var caminhoPdf = NomeArquivoUnico(pastaSplit, nomeBase, ".pdf");

            try
            {
                using var image = new MagickImage(imagem);
                image.Write(caminhoPdf, MagickFormat.Pdf);
                pdfsGerados.Add(caminhoPdf);
                _log.Debug($"PDF individual gerado: {Path.GetFileName(caminhoPdf)} (página original: {page})");
            }
            catch (Exception ex)
            {
                _log.Erro($"Erro ao converter imagem para PDF: {imagem}", ex);
            }
        }

        _log.Informacao($"Split concluído: {pdfsGerados.Count} PDF(s) individual(is) gerado(s)");
        return pdfsGerados;
    }

    public async Task<byte[]?> RenderizarPaginaPdfAsync(string caminhoPdf, int pagina = 0, int densidade = 200)
    {
        if (!File.Exists(caminhoPdf))
        {
            _log.Aviso($"PDF não encontrado para visualização: {caminhoPdf}");
            return null;
        }

        return await Task.Run(() =>
        {
            try
            {
                var settings = new MagickReadSettings
                {
                    Density = new Density(densidade, densidade),
                    Width = 1600
                };

                using var images = new MagickImageCollection(caminhoPdf, settings);

                if (images.Count == 0)
                    return null;

                var indice = Math.Clamp(pagina, 0, images.Count - 1);
                var image = images[indice];
                image.AutoOrient();

                return image.ToByteArray(MagickFormat.Png);
            }
            catch (Exception ex)
            {
                _log.Erro($"Erro ao renderizar página do PDF: {caminhoPdf}", ex);
                return null;
            }
        });
    }
}
