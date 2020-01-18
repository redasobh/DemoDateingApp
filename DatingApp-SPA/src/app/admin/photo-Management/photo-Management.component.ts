import { Component, OnInit } from '@angular/core';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-photo-Management',
  templateUrl: './photo-Management.component.html',
  styleUrls: ['./photo-Management.component.css']
})
export class PhotoManagementComponent implements OnInit {
photos: any;
  constructor(private adminservice: AdminService) { }

  ngOnInit() {
    this.getPhotosForApproval();
  }
getPhotosForApproval() {
  this.adminservice.getPhotosForApproval().subscribe((photos) => {
    this.photos = photos;
  }, error => {
    console.log(error);
  });
}
approvePhoto(photoId) {
  this.adminservice.approvePhoto(photoId).subscribe(() => {
    this.photos.splice(this.photos.findIndex(p => p.id === photoId), 1);
  }, error => {
    console.log(error);
  });
}
rejectPhoto(photoId) {
  this.adminservice.rejectPhoto(photoId).subscribe(() => {
    this.photos.splice(this.photos.findIndex(p => p.id === photoId), 1);
  }, error => {
    console.log(error);
  });
}
}
