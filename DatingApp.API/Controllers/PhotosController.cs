using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
   // [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _mapper = mapper;
            _repo = repo;
            _cloudinaryConfig = cloudinaryConfig;
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }
        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id){
            var photoFromRepo = await _repo.GetPhoto(id);
            var photo =_mapper.Map<PhotoForReturnDto>(photoFromRepo);
            return Ok(photo);
        }
        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreateDto photoForCreateDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();
                var userFromRepo = await _repo.GetUser(userId,true);
                var file = photoForCreateDto.File;
                var uploadResult = new ImageUploadResult();
                if (file!=null && file.Length > 0)
                {
                    using (var stream = file.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(file.Name, stream),
                            Transformation = new Transformation()
                            .Width(500).Height(500).Crop("fill").Gravity("face")
                        };
                        uploadResult = _cloudinary.Upload(uploadParams);
                    }
                }
                photoForCreateDto.Url = uploadResult.Uri.ToString();
                photoForCreateDto.PublicId = uploadResult.PublicId;
                var photo = _mapper.Map<Photo>(photoForCreateDto);
                if (!userFromRepo.Photos.Any(p => p.IsMain))
                    photo.IsMain = true;
                userFromRepo.Photos.Add(photo);
                if (await _repo.SaveAll())
                {
                    var PhotoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                    return CreatedAtRoute("GetPhoto", new { id = photo.Id }, PhotoToReturn);
                }
                return BadRequest("Cloud not Add The Photo");
        }
        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id){
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();
            var user = await _repo.GetUser(userId,true);
            if(!user.Photos.Any(p=>p.Id==id)) return Unauthorized();
            var photoFromRepo = await _repo.GetPhoto(id);
            if(photoFromRepo.IsMain) return BadRequest("this is Already the main Photo");
            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain=false;
            photoFromRepo.IsMain=true;
            if(await _repo.SaveAll()) return NoContent();
            return BadRequest("Cloud not set photo to main");
        }
        [HttpDelete("id")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();
            var user = await _repo.GetUser(userId,true);
            if(!user.Photos.Any(p=>p.Id==id)) return Unauthorized();
            var photoFromRepo = await _repo.GetPhoto(id);
            if(photoFromRepo.IsMain) return BadRequest("You Cannot delete your main Photo");
            if(photoFromRepo.PublicId!=null){
            var deleteParams = new DeletionParams(photoFromRepo.PublicId);
            var result = _cloudinary.Destroy(deleteParams);
            if(result.Result=="ok"){
                _repo.Delete(photoFromRepo);
            }
            }
            if(photoFromRepo.PublicId==null){
                _repo.Delete(photoFromRepo);
            }
            if(await _repo.SaveAll()) return Ok();
            return BadRequest("Failde to delete the photo");
        }
    }
}