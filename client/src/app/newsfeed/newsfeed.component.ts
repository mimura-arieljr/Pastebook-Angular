import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';
import { CurrentUserService } from '../current-user.service';

@Component({
  selector: 'app-newsfeed',
  templateUrl: './newsfeed.component.html',
  styleUrls: ['./newsfeed.component.scss']
})
export class NewsfeedComponent implements OnInit, AfterViewInit {
  @ViewChildren('lastPost', { read: ElementRef })
  lastPost!: QueryList<ElementRef>;


  newsfeedPosts: any = [];
  page: number = 1;
  isValidFileSize: boolean = true;
  newPostContent: string = '';
  base64Image: string = "";
  currentUser = {
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: '',
  };
  observer: any;

  constructor(
    private currentUserService: CurrentUserService,
    private http: HttpClient,
    private spinner: NgxSpinnerService
  ) { }

  ngOnInit(): void {
    this.currentUserService.currentUser$.subscribe((user) => {
      this.currentUser = user;
    })
    this.getNewsfeedPosts()
    this.intersectionObserver()
    setInterval(() => {
      this.autoUpdate();
    }, 60000)
  }

  ngAfterViewInit(): void {
    this.lastPost.changes.subscribe((d) => {
      if (d.last) this.observer.observe(d.last.nativeElement)
    })
  }

  handleFileSelect(evt: any) {
    var files = evt.target.files;
    var file = files[0];

    if (files && file) {
      var reader = new FileReader();

      reader.onload = (fileLoadedEvent: any) => {
        this.base64Image = fileLoadedEvent.target.result;
      }
      reader.readAsDataURL(file);
    }

  }

  autoUpdate() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };

      this.http.get<string>(`http://localhost:5000/autoupdate/${this.newsfeedPosts[0].Timestamp}`, httpOptions)
        .subscribe(recentPosts => {
          this.newsfeedPosts = [...recentPosts, ...this.newsfeedPosts]
        })
    }
  }

  resetCreatePost() {
    this.base64Image = '';
    this.newPostContent = '';
  }

  getNewsfeedPosts() {
    this.spinner.show()
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };
      this.http.get<string>(`http://localhost:5000/newsfeedposts/${this.page}`, httpOptions)
        .subscribe((newsfeedPosts) => {
          this.spinner.hide()
          this.newsfeedPosts = [...this.newsfeedPosts, ...newsfeedPosts]
        })
    }
  }

  intersectionObserver() {
    let options = {
      root: null,
      threshold: 0.5,
    }

    this.observer = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) {
        this.page++;
        this.getNewsfeedPosts()
      }
    }, options)
  }

  onSubmitPost() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };

      let newPost = {
        Timeline: 0,
        Content: this.newPostContent,
        ImageSrc: this.base64Image
      }
      this.http.post<string>(`http://localhost:5000/createpost/newsfeed`, newPost, httpOptions)
        .subscribe(recentPost => {
          this.newsfeedPosts = [recentPost, ...this.newsfeedPosts]
        })
    }
    this.resetCreatePost()
  }

  onDeletePost(id: number, index: number) {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        }),
        responseType: 'text' as 'json'
      };

      this.http.delete(`http://localhost:5000/posts/${id}`, httpOptions)
        .subscribe(() => {
          this.newsfeedPosts.splice(index, 1)
        })
    }
  }

  convertTimestampToRelative(timestamp: number) {
    let start: number = Date.now()
    let type; // s, m, h, w
    let timeValue;
    const timeSecondsAgo = Math.floor((start - timestamp) / 1000)
    if (timeSecondsAgo < 60) {
      type = 's'
      timeValue = (timeSecondsAgo).toString()
      return "A few seconds ago"
    }
    else if (timeSecondsAgo >= 60 && timeSecondsAgo < 3600) {
      type = 'm'
      timeValue = (timeSecondsAgo / 60).toString().split(".")[0]
      return timeValue + type
    }
    else if (timeSecondsAgo >= 3600 && timeSecondsAgo < 86400) {
      type = 'h'
      timeValue = (timeSecondsAgo / 60 / 60).toString().split(".")[0]
      return timeValue + type
    }
    else if (timeSecondsAgo >= 86400 && timeSecondsAgo < 604800) {
      type = 'd'
      timeValue = (timeSecondsAgo / 60 / 60 / 24).toString().split(".")[0]
      return timeValue + type
    }
    else if (timeSecondsAgo >= 604800) {
      type = 'w'
      timeValue = (timeSecondsAgo / 60 / 60 / 24 / 7).toString().split(".")[0]
      return timeValue + type
    }
    else {
      return "Invalid time"
    }
  }
}
