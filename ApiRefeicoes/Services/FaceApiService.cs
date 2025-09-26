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

        /// <summary>
        /// Deteta uma face numa imagem e retorna o seu faceId temporário.
        /// Este método é a base para a nossa lógica de verificação manual.
        /// </summary>
        public async Task<Guid?> DetectFace(byte[] imageBytes)
        {
            var client = GetClient();
            using var stream = new MemoryStream(imageBytes);

            try
            {
                var detectedFaces = await client.Face.DetectWithStreamAsync(stream, recognitionModel: _recognitionModel, detectionModel: DetectionModel.Detection03);

                if (detectedFaces.Any())
                {
                    var firstFace = detectedFaces.First();
                    _logger.LogInformation("Face detetada com sucesso. FaceId temporário: {FaceId}", firstFace.FaceId);
                    return firstFace.FaceId;
                }

                _logger.LogWarning("Nenhuma face foi detetada na imagem fornecida.");
                return null;
            }
            catch (APIErrorException apiEx)
            {
                _logger.LogError(apiEx, "ERRO NA API DO AZURE (Detecção): Status Code: {StatusCode}, Mensagem: {Message}", apiEx.Response.StatusCode, apiEx.Body?.Error?.Message);
                return null;
            }
        }

        /// <summary>
        /// Compara dois faceIds temporários para verificar se pertencem à mesma pessoa.
        /// </summary>
        public async Task<(bool isIdentical, double confidence)> VerifyFaces(Guid faceId1, Guid faceId2)
        {
            var client = GetClient();
            try
            {
                var result = await client.Face.VerifyFaceToFaceAsync(faceId1, faceId2);
                _logger.LogInformation("Resultado da verificação - IsIdentical: {IsIdentical}, Confidence: {Confidence}", result.IsIdentical, result.Confidence);
                return (result.IsIdentical, result.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar faces.");
                return (false, 0);
            }
        }

        // Os métodos de gestão de Person Group continuam a ser úteis para o processo de cadastro de colaboradores no portal.
        public async Task EnsurePersonGroupExistsAsync()
        {
            var client = GetClient();
            try { await client.PersonGroup.GetAsync(_personGroupId); }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                await client.PersonGroup.CreateAsync(_personGroupId, _personGroupId, recognitionModel: _recognitionModel);
            }
        }

        public async Task<Guid?> CreatePersonAsync(string name)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            var person = await client.PersonGroupPerson.CreateAsync(_personGroupId, name);
            return person.PersonId;
        }

        public async Task<PersistedFace> AddFaceToPersonAsync(Guid personId, byte[] imageBytes)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            using var stream = new MemoryStream(imageBytes);
            return await client.PersonGroupPerson.AddFaceFromStreamAsync(_personGroupId, personId, stream, detectionModel: DetectionModel.Detection03);
        }

        public async Task TrainPersonGroupAsync()
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            await client.PersonGroup.TrainAsync(_personGroupId);
            while (true)
            {
                await Task.Delay(1000);
                var status = await client.PersonGroup.GetTrainingStatusAsync(_personGroupId);
                if (status.Status is TrainingStatusType.Succeeded or TrainingStatusType.Failed) break;
            }
        }
    }
}