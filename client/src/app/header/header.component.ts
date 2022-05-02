import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CurrentUserService } from '../current-user.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
 user = {
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: '',
  };
  
  constructor(
    private currentUser: CurrentUserService,
    private router: Router) { }
  
  ngOnInit(): void {
    this.currentUser.currentUser$.subscribe((user) => {
      this.user = user
    })
  }

  onLogout() {
    this.currentUser.onLogout();
    this.router.navigateByUrl("/");
  }
}
