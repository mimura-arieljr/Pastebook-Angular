import { Component, OnInit } from '@angular/core';
import { CurrentUserService } from '../current-user.service';

@Component({
  selector: 'app-left-sidebar',
  templateUrl: './left-sidebar.component.html',
  styleUrls: ['./left-sidebar.component.scss']
})
export class LeftSidebarComponent implements OnInit {
  user = {
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: '',
  };
  
  constructor(private getCurrentUser: CurrentUserService) { }

  ngOnInit(): void {
    this.getCurrentUser.currentUser$.subscribe((user) => {
      this.user = user
    })
  }

}
