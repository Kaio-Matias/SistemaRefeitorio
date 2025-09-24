using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Net;

namespace ApiRefeicoes.Services
{
    public class FaceApiService
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _personGroupId;
        private readonly ILogger<FaceApiService> _logger;

        public FaceApiService(IConfiguration configuration, ILogger<FaceApiService> logger)
        {
            _apiKey = configuration["AzureFaceApi:ApiKey"];
            _endpoint = configuration["AzureFaceApi:Endpoint"];
            _personGroupId = configuration["AzureFaceApi:PersonGroupId"];
            _logger = logger;
        }

        private IFaceClient GetClient()
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(_apiKey)) { Endpoint = _endpoint };
        }

        public async Task<string> DetectFaceAndGetId(byte[] imageBytes)
        {
            var client = GetClient();
            using var stream = new MemoryStream(imageBytes);

            try
            {
                // Passo 1: Tentar DETECTAR um rosto na imagem
                var detectedFaces = await client.Face.DetectWithStreamAsync(stream, recognitionModel: RecognitionModel.Recognition04, detectionModel: DetectionModel.Detection03);

                if (detectedFaces.Count == 0)
                {
                    _logger.LogWarning("PASSO 1 FALHOU: Nenhuma face foi detectada na imagem pelo Azure.");
                    return null;
                }

                _logger.LogInformation("PASSO 1 SUCESSO: {Count} face(s) detectada(s). FaceId temporário: {FaceId}", detectedFaces.Count, detectedFaces.First().FaceId);

                var faceIds = detectedFaces.Select(f => f.FaceId.Value).ToList();

                // Passo 2: Tentar IDENTIFICAR o rosto detectado no nosso grupo de pessoas
                var identifyResults = await client.Face.IdentifyAsync(faceIds, _personGroupId);

                if (identifyResults == null || !identifyResults.Any())
                {
                    _logger.LogWarning("PASSO 2 FALHOU: A API de Identificação não retornou resultados.");
                    return null;
                }

                foreach (var result in identifyResults)
                {
                    if (result.Candidates.Count > 0)
                    {
                        var candidate = result.Candidates.OrderByDescending(c => c.Confidence).First();
                        var personId = candidate.PersonId;
                        _logger.LogInformation("PASSO 2 SUCESSO: Face identificada como PersonId (AzureId): {PersonId} com confiança de {Confidence}", personId, candidate.Confidence);
                        return personId.ToString();
                    }
                }

                _logger.LogWarning("PASSO 2 FALHOU: Uma face foi detectada, mas não corresponde a nenhum colaborador cadastrado no Azure (nenhum candidato encontrado).");
                return null;
            }
            catch (APIErrorException apiEx)
            {
                _logger.LogError(apiEx, "ERRO NA API DO AZURE: Status Code: {StatusCode}, Mensagem: {Message}", apiEx.Response.StatusCode, apiEx.Body?.Error?.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no FaceApiService ao tentar detectar e identificar.");
                return null;
            }
        }

        public async Task<(bool isIdentical, double confidence)> VerifyFaces(Guid faceId1, Guid faceId2)
        {
            var client = GetClient();
            try
            {
                var result = await client.Face.VerifyFaceToFaceAsync(faceId1, faceId2);
                return (result.IsIdentical, result.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar faces.");
                return (false, 0);
            }
        }

        public async Task<Guid?> CreatePerson(string name)
        {
            var client = GetClient();
            try
            {
                var person = await client.PersonGroupPerson.CreateAsync(_personGroupId, name);
                return person.PersonId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar pessoa no grupo.");
                return null;
            }
        }

        public async Task<PersistedFace> AddFaceToPerson(Guid personId, byte[] imageBytes)
        {
            var client = GetClient();
            using var stream = new MemoryStream(imageBytes);
            try
            {
                var persistedFace = await client.PersonGroupPerson.AddFaceFromStreamAsync(_personGroupId, personId, stream, detectionModel: DetectionModel.Detection03);
                return persistedFace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar face à pessoa.");
                return null;
            }
        }

        public async Task TrainPersonGroup()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.TrainAsync(_personGroupId);

                while (true)
                {
                    var status = await client.PersonGroup.GetTrainingStatusAsync(_personGroupId);
                    if (status.Status == TrainingStatusType.Succeeded || status.Status == TrainingStatusType.Failed)
                    {
                        _logger.LogInformation("Treinamento do grupo de pessoas concluído com status: {Status}", status.Status);
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao treinar o grupo de pessoas.");
            }
        }
    }
}