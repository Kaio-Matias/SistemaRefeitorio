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
        private readonly string _recognitionModel;
        private readonly ILogger<FaceApiService> _logger;

        public FaceApiService(IConfiguration configuration, ILogger<FaceApiService> logger)
        {
            _apiKey = configuration["AzureFaceApi:ApiKey"];
            _endpoint = configuration["AzureFaceApi:Endpoint"];
            _personGroupId = configuration["AzureFaceApi:PersonGroupId"];
            _recognitionModel = configuration["AzureFaceApi:RecognitionModel"];
            _logger = logger;
        }

        private IFaceClient GetClient()
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(_apiKey)) { Endpoint = _endpoint };
        }

        public async Task<Guid?> DetectFace(Stream imageStream)
        {
            var client = GetClient();
            try
            {
                // Garante que o stream possa ser lido múltiplas vezes se necessário
                if (!imageStream.CanSeek)
                {
                    var memoryStream = new MemoryStream();
                    await imageStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    imageStream = memoryStream;
                }
                else
                {
                    imageStream.Position = 0;
                }

                var detectedFaces = await client.Face.DetectWithStreamAsync(imageStream, recognitionModel: _recognitionModel, detectionModel: DetectionModel.Detection03);
                if (detectedFaces.Any())
                {
                    var firstFace = detectedFaces.First();
                    _logger.LogInformation("Face detetada com sucesso. FaceId: {FaceId}", firstFace.FaceId);
                    return firstFace.FaceId;
                }
                _logger.LogWarning("Nenhuma face detetada na imagem.");
                return null;
            }
            catch (APIErrorException apiEx)
            {
                _logger.LogError(apiEx, "Erro na API do Azure (Detetar): {Message}", apiEx.Body?.Error?.Message);
                return null;
            }
        }

        public async Task<Guid?> IdentifyFaceAsync(Guid faceId)
        {
            var client = GetClient();
            try
            {
                var identifyResults = await client.Face.IdentifyAsync(new List<Guid> { faceId }, _personGroupId);
                var result = identifyResults.FirstOrDefault()?.Candidates.FirstOrDefault();
                if (result != null && result.Confidence > 0.5) // Limiar de confiança
                {
                    _logger.LogInformation("Pessoa identificada: {PersonId} com confiança {Confidence}", result.PersonId, result.Confidence);
                    return result.PersonId;
                }
                return null;
            }
            catch (APIErrorException apiEx)
            {
                _logger.LogError(apiEx, "Erro na API do Azure (Identificar): {Message}", apiEx.Body?.Error?.Message);
                return null;
            }
        }

        public async Task EnsurePersonGroupExistsAsync()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.GetAsync(_personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Grupo de Pessoas '{PersonGroupId}' não encontrado. A criar um novo.", _personGroupId);
                await client.PersonGroup.CreateAsync(_personGroupId, _personGroupId, recognitionModel: _recognitionModel);
            }
        }

        public async Task<Guid> CreatePersonAsync(string name)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            var person = await client.PersonGroupPerson.CreateAsync(_personGroupId, name);
            return person.PersonId;
        }

        public async Task<PersistedFace> AddFaceToPersonAsync(Guid personId, Stream imageStream)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            imageStream.Position = 0;
            return await client.PersonGroupPerson.AddFaceFromStreamAsync(_personGroupId, personId, imageStream, detectionModel: DetectionModel.Detection03);
        }

        public async Task TrainPersonGroupAsync()
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            _logger.LogInformation("A iniciar treino para o grupo: {PersonGroupId}", _personGroupId);
            await client.PersonGroup.TrainAsync(_personGroupId);
        }

        // --- MÉTODOS DE DEBUG RESTAURADOS ---

        public async Task<TrainingStatus> GetTrainingStatusAsync()
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            return await client.PersonGroup.GetTrainingStatusAsync(_personGroupId);
        }

        public async Task DeletePersonGroupAsync()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.DeleteAsync(_personGroupId);
                _logger.LogInformation("Grupo de Pessoas '{PersonGroupId}' apagado com sucesso.", _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Tentativa de apagar o Grupo de Pessoas '{PersonGroupId}', mas ele não foi encontrado.", _personGroupId);
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

        public async Task<string> DetectFaceAndGetId(Stream imageStream)
        {
            Guid? faceId = await DetectFace(imageStream);
            return faceId?.ToString();
        }
        public async Task DeletePersonAsync(Guid personId)
        {
            if (personId == Guid.Empty)
            {
                _logger.LogWarning("Tentativa de apagar uma pessoa com PersonId vazio (Guid.Empty).");
                return;
            }

            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            try
            {
                await client.PersonGroupPerson.DeleteAsync(_personGroupId, personId);
                _logger.LogInformation("Pessoa com PersonId {PersonId} foi apagada com sucesso do grupo.", personId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Tentativa de apagar a pessoa com PersonId {PersonId}, mas ela não foi encontrada no grupo da Azure.", personId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao apagar a pessoa com PersonId {PersonId} do grupo da Azure.", personId);
                throw;
            }
        }
    }
}