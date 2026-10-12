"""Read-only diagnostics. No phone connection or commands, no success inference."""
import argparse,csv,json,re
from datetime import datetime
from pathlib import Path

def analyze_jsonl(path):
    gestures={};warnings=[];session=None;last_seq=0;records=0
    with path.open(encoding='utf-8-sig') as stream:
        for line_no,line in enumerate(stream,1):
            try:r=json.loads(line)
            except (ValueError,TypeError):warnings.append(f'line {line_no}: invalid/partial JSON');continue
            records+=1
            if r.get('schema')!=1:warnings.append(f'line {line_no}: unknown schema retained, not interpreted');continue
            if session is None:session=r.get('session')
            if r.get('session')!=session:warnings.append(f'line {line_no}: mixed session')
            seq=r.get('seq')
            if isinstance(seq,int):
                if seq!=last_seq+1:warnings.append(f'line {line_no}: sequence gap/order {last_seq}->{seq}')
                last_seq=seq
            if r.get('dropped',0):warnings.append(f'line {line_no}: dropped {r["dropped"]}')
            gid=r.get('gesture',0)
            if not isinstance(gid,int) or gid<=0:continue
            g=gestures.setdefault(gid,dict(id=gid,source='jsonl',first=r.get('at'),last=r.get('at'),
                input_samples=0,coalesced=0,absolute_reports=0,relative_reports=0,cancelled=False,
                errors=[],start=None,end=None,phone_effect='unverified'))
            g['last']=r.get('at');kind=r.get('kind');data=r.get('data',{})
            if not isinstance(data,dict):warnings.append(f'line {line_no}: unknown data shape');continue
            if kind=='physical-input':g['input_samples']+=1
            if kind=='coalesced':g['coalesced']+=1
            if kind=='absolute-report':
                g['absolute_reports']+=1
                coord=[data.get('x'),data.get('y')]
                if g['start'] is None:g['start']=coord
                g['end']=coord
            if kind=='relative-report':g['relative_reports']+=1
            if kind=='cancel' or data.get('cancelled'):g['cancelled']=True
            if kind in ('send-error','api-error','send-skipped','button-skipped'):g['errors'].append({'kind':kind,'data':data})
    return dict(source=str(path),format='jsonl',session=session,records=records,warnings=warnings,gestures=list(gestures.values()))

def analyze_legacy(path):
    gestures=[];active=None;warnings=[];lines=0
    for line in path.read_text(encoding='utf-8-sig',errors='replace').splitlines():
        lines+=1
        m=re.match(r'^(\S+) (.*)$',line)
        if not m:continue
        at,msg=m.groups()
        if '物理输入：按钮位' in msg:
            if active and not active.get('released'):active['uncertainty']='overlapping input or incomplete previous gesture'
            active=dict(id=len(gestures)+1,source='legacy',first=at,last=at,start=None,end=None,
                button=None,bottom=None,released=False,cancelled=False,phone_effect='unverified',
                input_samples=None,coalesced=None,absolute_reports=None,relative_reports=None)
            gestures.append(active)
        if active is None:continue
        active['last']=at
        p=re.search(r'配置按钮(\d+)，起点(\d+),(\d+)，底部起手(True|False)',msg)
        if p:active.update(button=int(p[1]),physical_start=[int(p[2]),int(p[3])],bottom=p[4]=='True')
        p=re.search(r'拖动按下实际坐标：(\d+),(\d+)',msg)
        if p:active['start']=[int(p[1]),int(p[2])]
        p=re.search(r'拖动松开，终点(\d+),(\d+)',msg)
        if p:active['end']=[int(p[1]),int(p[2])]
        if '轻点确认' in msg:active['action']='tap'
        p=re.search(r'按钮报告：(\d+) → (\d+)',msg)
        if p and int(p[1])>0 and int(p[2])==0:
            active['released']=True;active['release_at']=at;active=None
        elif any(x in msg for x in ('鼠标已离开','蓝牙断开','同步控制已暂停','输入发送积压','已恢复电脑鼠标')):
            active['cancelled']=True;active['cancel_reason']=msg;active=None
    warnings.append('Legacy log has no sample IDs, full motion, build identity or phone execution evidence; unknown values remain null.')
    return dict(source=str(path),format='legacy',records=lines,warnings=warnings,gestures=gestures)

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('log',type=Path);ap.add_argument('--out',type=Path,required=True)
    args=ap.parse_args();path=args.log.resolve()
    report=analyze_jsonl(path) if path.suffix.lower()=='.jsonl' else analyze_legacy(path)
    args.out.mkdir(parents=True,exist_ok=True)
    stem='gesture-report-'+datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    target=args.out/(stem+'.json')
    target.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    fields=['id','source','first','last','start','end','button','bottom','released','cancelled','input_samples','coalesced','absolute_reports','relative_reports','phone_effect']
    with (args.out/(stem+'.csv')).open('w',newline='',encoding='utf-8-sig') as stream:
        writer=csv.DictWriter(stream,fieldnames=fields,extrasaction='ignore');writer.writeheader();writer.writerows(report['gestures'])
    print(json.dumps(dict(report=str(target.resolve()),gestures=len(report['gestures']),warnings=report['warnings']),ensure_ascii=False))

if __name__=='__main__':main()
