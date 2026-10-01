# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const DECK_POSITION = Vector2(0, 170)

func animate() -> void:
	while await deal():
		pass

func deal() -> bool:
	for card in targets.deck:
		card.body.position = DECK_POSITION
		card.body.rotation = 0.0
		card.body.scale = Vector2.ONE
		card.body.modulate = Color.WHITE
		show_face(card, false)
	if not await spread(): return false
	if not await flip_all(true, 0.09 * tempo): return false
	if not await lift_hero(): return false
	if not await wait(0.6 * tempo): return false
	if not await gather(): return false
	if not await flip_all(false, 0.0): return false
	return await toss()

func show_face(card: Dictionary, face_up: bool) -> void:
	card.back.visible = not face_up
	card.face.visible = face_up

# Each flip has its own lifetime-bound completion handle. Start all before awaiting.
func flip_all(face_up: bool, stagger: float) -> bool:
	var completions: Array = []
	for i in targets.deck.size():
		var completion = FlipCompletion.new()
		completions.append(completion)
		flip(targets.deck[i], i * stagger, face_up, completion)
	var successful = true
	for completion in completions:
		if not await completion.wait(): successful = false
	return successful

class FlipCompletion:
	extends RefCounted
	signal ended
	var settled = false
	var successful = false

	func finish(result: bool) -> void:
		successful = result
		settled = true
		ended.emit()

	func wait() -> bool:
		if not settled: await ended
		return successful

func flip(card: Dictionary, delay: float, face_up: bool, completion: FlipCompletion) -> void:
	var fold = options(Tweens.scale_2d_x(0.0, 0.1 * tempo), In.QUAD, null, delay)
	fold.on_end = func(_h): show_face(card, face_up)
	var result = await Tweens.chain(card.body, [
		fold,
		options(Tweens.scale_2d_x(1.0, 0.22 * tempo), Tweens.Ease.BACK_OUT),
	]).end
	completion.finish(result == Tweens.Reason.COMPLETED)

func spread() -> bool:
	var handles: Array = []
	for i in targets.deck.size():
		var card = targets.deck[i].body
		var offset = i - (targets.deck.size() - 1) / 2.0
		var spot = Vector2(offset * 64.0, abs(offset) * 7.0 + 4.0)
		handles.append(Tweens.play(card, options(Tweens.position_2d(spot, 0.5 * tempo), Tweens.Ease.BACK_OUT, null, i * 0.1 * tempo)))
		handles.append(Tweens.play(card, options(Tweens.rotation_2d(offset * 0.13, 0.5 * tempo), Tweens.Ease.BACK_OUT, null, i * 0.1 * tempo)))
	return await Tweens.group(handles).wait() == Tweens.Reason.COMPLETED

func lift_hero() -> bool:
	var hero = targets.deck[-1].body
	return await Tweens.group([
		Tweens.play(hero, options(Tweens.position_2d_y(hero.position.y - 26.0, 0.35 * tempo), Tweens.Ease.BACK_OUT)),
		Tweens.play(hero, options(Tweens.scale_2d([1.18, 1.18], 0.35 * tempo), Tweens.Ease.BACK_OUT)),
		Tweens.play(hero, options(Tweens.rotation_2d(0.0, 0.35 * tempo), Tweens.Ease.BACK_OUT)),
	]).wait() == Tweens.Reason.COMPLETED

func gather() -> bool:
	var handles: Array = []
	for i in targets.deck.size():
		var card = targets.deck[i].body
		var delay = (targets.deck.size() - 1 - i) * 0.05 * tempo
		for definition in [Tweens.position_2d([0, -i * 2], 0.35 * tempo), Tweens.rotation_2d(0.0, 0.35 * tempo), Tweens.scale_2d(Vector2.ONE, 0.35 * tempo)]:
			handles.append(Tweens.play(card, options(definition, InOut.CUBIC, null, delay)))
	return await Tweens.group(handles).wait() == Tweens.Reason.COMPLETED

func toss() -> bool:
	var handles: Array = []
	for i in targets.deck.size():
		var card = targets.deck[i].body
		handles.append(Tweens.play(card, options(Tweens.position_2d([(i - 2) * 30, -170], 0.45 * tempo), Tweens.Ease.BACK_IN, null, i * 0.04 * tempo)))
		handles.append(Tweens.play(card, options(Tweens.rotation_2d((i - 2) * 0.4, 0.45 * tempo), Tweens.Ease.BACK_IN, null, i * 0.04 * tempo)))
	return await Tweens.group(handles).wait() == Tweens.Reason.COMPLETED

func options(definition, easing = InOut.LINEAR, from = null, delay: float = 0.0):
	definition.ease = easing
	definition.from_value = from
	definition.delay = delay
	return definition

func wait(seconds_to_wait: float) -> bool:
	return await Tweens.play(stage, Tweens.value(0.0, 1.0, seconds_to_wait)).wait() == Tweens.Reason.COMPLETED
