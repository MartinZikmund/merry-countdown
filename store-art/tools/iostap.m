// Taps and swipes on a booted iOS Simulator without Simulator.app or idb (Xcode 27 SimulatorKit).
// Build: clang -fobjc-arc -framework Foundation -framework CoreGraphics iostap.m -o iostap
// Use:   iostap <udid> tap x y [x y ...] | swipe x1 y1 x2 y2   (0..1 screen ratios; HOLD_MS for long presses)
#import <Foundation/Foundation.h>
#import <CoreGraphics/CoreGraphics.h>
#import <objc/message.h>
#include <dlfcn.h>
#include <malloc/malloc.h>

typedef void *(*MouseFn)(CGPoint *p0, CGPoint *p1, uint32_t target, uint32_t type, BOOL flag);

int main(int argc, const char **argv) {
  @autoreleasepool {
    NSString *xcode = [NSProcessInfo.processInfo.environment[@"XCODE_DEV"] ?: @"/Applications/Xcode-27.2.0.app/Contents/Developer" copy];
    dlopen("/Library/Developer/PrivateFrameworks/CoreSimulator.framework/CoreSimulator", RTLD_NOW);
    void *sk = dlopen((getenv("SIMKIT") ?: "/Applications/Xcode-27.2.0.app/Contents/SharedFrameworks/SimulatorKit.framework/Versions/A/SimulatorKit"), RTLD_NOW);
    if (!sk) { fprintf(stderr, "dlopen: %s\n", dlerror()); return 1; }
    MouseFn mouse = (MouseFn)dlsym(sk, "IndigoHIDMessageForMouseNSEvent");
    if (!mouse) { fprintf(stderr, "no mouse fn\n"); return 1; }

    if (strcmp(argv[2], "dump") == 0) {
      CGPoint p = CGPointMake(atof(argv[3]), atof(argv[4]));
      int ts[] = {1, 6, 2}; for (int ti = 0; ti < 3; ti++) { int t = ts[ti];
        unsigned char *m = mouse(&p, NULL, 0x32, t, NO);
        size_t n = malloc_size(m);
        printf("type %d size %zu\n", t, n);
        for (size_t i = 0; i < n; i++) { printf("%02x%s", m[i], (i % 16 == 15) ? "\n" : " "); }
        printf("\n");
      }
      return 0;
    }

    id ctx = ((id(*)(id, SEL, id, id *))objc_msgSend)(NSClassFromString(@"SimServiceContext"), sel_registerName("sharedServiceContextForDeveloperDir:error:"), xcode, nil);
    id set = ((id(*)(id, SEL, id *))objc_msgSend)(ctx, sel_registerName("defaultDeviceSetWithError:"), nil);
    NSDictionary *devs = ((id(*)(id, SEL))objc_msgSend)(set, sel_registerName("devicesByUDID"));
    id dev = devs[[[NSUUID alloc] initWithUUIDString:[NSString stringWithUTF8String:argv[1]]]];
    if (!dev) { fprintf(stderr, "device not found\n"); return 1; }
    NSError *err = nil;
    id client = ((id(*)(id, SEL, id, NSError **))objc_msgSend)([NSClassFromString(@"SimulatorKit.SimDeviceLegacyHIDClient") alloc], sel_registerName("initWithDevice:error:"), dev, &err);
    if (!client) { fprintf(stderr, "HID client: %s\n", err.description.UTF8String); return 1; }
    dispatch_queue_t q = dispatch_queue_create("hid", DISPATCH_QUEUE_SERIAL);
    void (^send)(void *) = ^(void *msg) {
      dispatch_semaphore_t s = dispatch_semaphore_create(0);
      ((void(*)(id, SEL, void *, BOOL, dispatch_queue_t, void (^)(NSError *)))objc_msgSend)(client, sel_registerName("sendWithMessage:freeWhenDone:completionQueue:completion:"), msg, YES, q, ^(NSError *e) {
        if (e) { fprintf(stderr, "send: %s\n", e.description.UTF8String); }
        dispatch_semaphore_signal(s);
      });
      dispatch_semaphore_wait(s, DISPATCH_TIME_FOREVER);
    };
    void *(^msg)(CGPoint, int) = ^void *(CGPoint p, int t) {
      unsigned char *m = mouse(&p, NULL, 0x32, t, NO);
      for (int k = 0; k < 2; k++) { *(double *)(m + 0x3c + k * 0xa0) = p.x; *(double *)(m + 0x44 + k * 0xa0) = p.y; }
      return m;
    };
    if (strcmp(argv[2], "swipe") == 0) {
      CGPoint a = CGPointMake(atof(argv[3]), atof(argv[4])), b = CGPointMake(atof(argv[5]), atof(argv[6]));
      send(msg(a, 1));
      for (int i = 1; i <= 20; i++) {
        CGPoint p = CGPointMake(a.x + (b.x - a.x) * i / 20, a.y + (b.y - a.y) * i / 20);
        send(msg(p, 1));
        usleep(15000);
      }
      usleep(150000);
      send(msg(b, 2));
      usleep(500000);
      return 0;
    }
    int hold = getenv("HOLD_MS") ? atoi(getenv("HOLD_MS")) : 90;
    for (int i = 3; i + 1 < argc; i += 2) {
      CGPoint p = CGPointMake(atof(argv[i]), atof(argv[i + 1]));
      for (int t = 1; t <= 2; t++) {
        unsigned char *m = mouse(&p, NULL, 0x32, t, NO);
        for (int k = 0; k < 2; k++) {
          *(double *)(m + 0x3c + k * 0xa0) = p.x;
          *(double *)(m + 0x44 + k * 0xa0) = p.y;
        }
        send(m);
        if (t == 1) { usleep(hold * 1000); }
      }
      usleep(600 * 1000);
    }
  }
  return 0;
}
