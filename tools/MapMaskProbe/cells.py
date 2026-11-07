import sys,struct,zlib
P='C:/Users/PC/AppData/Local/Temp/claude/1d5348fd6631f675/79effd41-8318-4047-87d8-d463d53d7b0d/scratchpad/p11/'
def load(name):
    b=open(P+name,'rb').read();return b
def png(path,w,h,rgb):
    raw=b''.join(b'\x00'+bytes(rgb[y*w*3:(y+1)*w*3]) for y in range(h))
    def chunk(t,d):return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
    open(path,'wb').write(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))
for flat,flag in (('f1f100',16382),('x6f200',2147483646)):
    d=load(flat+'d.bgra');W=512
    def px(x,y,ch):  # ch 0=R,1=G,2=B ; data BGRA
        i=(y*W+x)*4;return d[i+2-ch]
    regions=[r for r in range(32) if flag>>r&1]
    T=128
    union=[[0]*T for _ in range(T)];per={}
    for r in regions:
        c=r//3;ch=r%3;cx=(c%4)*T;cy=(c//4)*T;n=0
        for v in range(T):
            for u in range(T):
                if px(cx+u,cy+v,ch)>127:union[v][u]+=1;n+=1
        per[r]=n
    # silhouette: cell 0 red channel low = inside the map?
    inside=sum(1 for v in range(T) for u in range(T) if px(u,v,0)<128)
    covered=sum(1 for v in range(T) for u in range(T) if union[v][u]>0)
    inside_cov=sum(1 for v in range(T) for u in range(T) if union[v][u]>0 and px(u,v,0)<128)
    overlap=sum(1 for v in range(T) for u in range(T) if union[v][u]>1)
    print(flat,'regions',len(regions),'pixels per region',per)
    print('  silhouette (cell0 R<128):',inside,' union:',covered,' union inside silhouette:',inside_cov,' overlaps:',overlap)
    # picture: silhouette grey, union coloured by region
    rgb=[]
    import colorsys
    for v in range(T):
        for u in range(T):
            hit=[r for r in regions if px((r//3%4)*T+u,(r//3//4)*T+v,r%3)>127]
            if hit:
                h=(hit[0]*0.13)%1;cr,cg,cb=colorsys.hsv_to_rgb(h,0.8,1);rgb+= [int(cr*255),int(cg*255),int(cb*255)]
            elif px(u,v,0)<128:rgb+=[80,80,80]
            else:rgb+=[0,0,0]
    big=[];S=4
    for v in range(T*S):
        for u in range(T*S):
            i=((v//S)*T+(u//S))*3;big+=rgb[i:i+3]
    png(P+flat+'-regions.png',T*S,T*S,big)
